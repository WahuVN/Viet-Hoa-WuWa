using VHWuWa.Core.Models;
using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class ViethoaInstallerTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "vhw_viet_" + Guid.NewGuid().ToString("N"));
    private readonly string _game;
    private readonly string _content;
    private readonly string _win64;
    private readonly string _paks;
    private readonly SettingsService _settings;
    private readonly LogService _log;
    private readonly ViethoaInstaller _viet;

    public ViethoaInstallerTests()
    {
        _game = Path.Combine(_work, "Wuthering Waves Game");
        _content = Path.Combine(_work, "content");
        _win64 = Path.Combine(_game, "Client", "Binaries", "Win64");
        _paks = Path.Combine(_game, "Client", "Content", "Paks");
        Directory.CreateDirectory(_win64);
        Directory.CreateDirectory(_paks);
        Directory.CreateDirectory(Path.Combine(_content, "loader"));
        Directory.CreateDirectory(Path.Combine(_content, "font"));

        // Game giả lập
        File.WriteAllText(Path.Combine(_win64, "Client-Win64-Shipping.exe"), "exe");
        File.WriteAllText(Path.Combine(_paks, "pakchunk0optional-WindowsNoEditor.sig"), "SEEDSIG");

        // Nội dung Việt hóa giả lập
        File.WriteAllText(Path.Combine(_content, "WuWaVH_HanViet_99_P.pak"), "HANVIET");
        File.WriteAllText(Path.Combine(_content, "WuWaVH_EN_99_P.pak"), "ENGLISH");
        File.WriteAllText(Path.Combine(_content, "loader", "version.dll"), "LOADER_VERSION");
        File.WriteAllText(Path.Combine(_content, "loader", "verorg.dll"), "VERORG");
        File.WriteAllText(Path.Combine(_content, "loader", "WuWaVH.dll"), "SDKDLL");
        File.WriteAllText(Path.Combine(_content, "font", "WahuFont_100_P.pak"), "FONT");

        _settings = new SettingsService(Path.Combine(_work, "appdata"));
        _log = new LogService(_settings);
        _viet = new ViethoaInstaller(_log, _content);
    }

    private string Mods => Path.Combine(_paks, "~WuWaMods");

    [Fact]
    public void InspectContent_Ready()
    {
        var c = _viet.InspectContent();
        Assert.True(c.HasHanViet);
        Assert.True(c.HasEnglish);
        Assert.True(c.HasLoader);
        Assert.NotNull(c.FontPak);
        Assert.True(c.Ready);
    }

    [Fact]
    public async Task Install_HanViet_PlacesPakSigLoaderAndBackup()
    {
        var r = await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true);
        Assert.True(r.Success, r.Error);

        // Pak + .sig trong ~WuWaMods
        Assert.Equal("HANVIET", File.ReadAllText(Path.Combine(Mods, "WuWaVH_99_P.pak")));
        Assert.True(File.Exists(Path.Combine(Mods, "WuWaVH_99_P.sig")));
        Assert.Equal("SEEDSIG", File.ReadAllText(Path.Combine(Mods, "WuWaVH_99_P.sig"))); // copy từ .sig gốc
        // Font + .sig
        Assert.Equal("FONT", File.ReadAllText(Path.Combine(Mods, "WahuFont_100_P.pak")));
        Assert.True(File.Exists(Path.Combine(Mods, "WahuFont_100_P.sig")));
        // Loader ở Win64
        Assert.Equal("LOADER_VERSION", File.ReadAllText(Path.Combine(_win64, "version.dll")));
        Assert.True(File.Exists(Path.Combine(_win64, "verorg.dll")));
        Assert.True(File.Exists(Path.Combine(_win64, "WuWaVH.dll")));
        Assert.False(File.Exists(Path.Combine(_win64, "version_goc.dll")));

        var st = _viet.GetStatus(_game);
        Assert.True(st.Installed);
        Assert.Equal("hanviet", st.Variant);
    }

    [Fact]
    public async Task SwitchVariant_EN_Overwrites()
    {
        await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: false);
        var r = await _viet.InstallAsync(_game, NameVariant.English, withFont: false);
        Assert.True(r.Success, r.Error);
        Assert.Equal("ENGLISH", File.ReadAllText(Path.Combine(Mods, "WuWaVH_99_P.pak")));
        Assert.Equal("en", _viet.GetStatus(_game).Variant);
    }

    [Fact]
    public async Task Uninstall_RemovesOwnedModAndLoader()
    {
        await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true);
        var r = await _viet.UninstallAsync(_game);
        Assert.True(r.Success, r.Error);

        Assert.False(Directory.Exists(Mods));                                  // ~WuWaMods bị xóa
        Assert.False(File.Exists(Path.Combine(_win64, "WuWaVH.dll")));         // loader gỡ
        Assert.False(File.Exists(Path.Combine(_win64, "verorg.dll")));
        Assert.False(File.Exists(Path.Combine(_win64, "version_goc.dll")));    // backup dọn
        Assert.False(File.Exists(Path.Combine(_win64, "version.dll")));
        Assert.False(_viet.GetStatus(_game).Installed);
    }

    [Fact]
    public async Task ExistingProxyLoader_BlocksInstallWithoutOverwrite()
    {
        var proxy = Path.Combine(_win64, "version.dll");
        File.WriteAllText(proxy, "OTHER_MOD_LOADER");

        var conflicts = _viet.FindConflicts(_game);
        Assert.Contains(conflicts, x => x.Contains("version.dll", StringComparison.OrdinalIgnoreCase));
        var result = await _viet.InstallAsync(_game, NameVariant.English, withFont: true);

        Assert.False(result.Success);
        Assert.Equal("OTHER_MOD_LOADER", File.ReadAllText(proxy));
        Assert.False(Directory.Exists(Mods));
    }

    [Fact]
    public async Task ExistingPakInOtherModFolder_BlocksInstallWithoutDeletingIt()
    {
        var otherDir = Path.Combine(_paks, "~mods");
        Directory.CreateDirectory(otherDir);
        var otherPak = Path.Combine(otherDir, "OtherMod_P.pak");
        File.WriteAllText(otherPak, "OTHER_MOD");

        var result = await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true);

        Assert.False(result.Success);
        Assert.Equal("OTHER_MOD", File.ReadAllText(otherPak));
        Assert.False(Directory.Exists(Mods));
    }

    [Fact]
    public async Task OwnInstall_IsNotReportedAsConflict_AndCanSwitchVariant()
    {
        Assert.True((await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true)).Success);
        Assert.Empty(_viet.FindConflicts(_game));

        var result = await _viet.InstallAsync(_game, NameVariant.English, withFont: true);

        Assert.True(result.Success, result.Error);
        Assert.Equal("ENGLISH", File.ReadAllText(Path.Combine(Mods, "WuWaVH_99_P.pak")));
    }

    [Fact]
    public async Task Uninstall_PreservesUnknownFilePlacedBesideOwnedFiles()
    {
        Assert.True((await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true)).Success);
        var foreign = Path.Combine(Mods, "KeepMe.txt");
        File.WriteAllText(foreign, "USER_FILE");

        var result = await _viet.UninstallAsync(_game);

        Assert.True(result.Success, result.Error);
        Assert.True(Directory.Exists(Mods));
        Assert.Equal("USER_FILE", File.ReadAllText(foreign));
        Assert.False(File.Exists(Path.Combine(Mods, "WuWaVH_99_P.pak")));
    }

    [Fact]
    public async Task Install_WrongFolder_Fails()
    {
        var bad = Path.Combine(_work, "notgame");
        Directory.CreateDirectory(bad);
        var r = await _viet.InstallAsync(bad, NameVariant.HanViet, withFont: false);
        Assert.False(r.Success);
    }

    public void Dispose()
    {
        _log.Dispose();
        try { if (Directory.Exists(_work)) Directory.Delete(_work, true); } catch { }
    }
}
