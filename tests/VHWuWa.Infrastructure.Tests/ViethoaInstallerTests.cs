using System.Text.Json.Nodes;
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
        _viet = new ViethoaInstaller(_log, _content, Path.Combine(_work, "quarantine"),
            isGameRunning: () => false);
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
        var assemblyVersion = typeof(ViethoaInstaller).Assembly.GetName().Version!;
        Assert.Equal($"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}", st.Version);

        var marker = File.ReadAllText(Path.Combine(Mods, "vhwuwa_install.json"));
        Assert.Contains($"\"packageVersion\": \"{st.Version}\"", marker, StringComparison.Ordinal);
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
    public async Task LegacyMarkerWithoutVersion_UsesCurrentApplicationVersion()
    {
        Assert.True((await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: false)).Success);
        var markerPath = Path.Combine(Mods, "vhwuwa_install.json");
        var marker = JsonNode.Parse(File.ReadAllText(markerPath))!.AsObject();
        marker.Remove("packageVersion");
        File.WriteAllText(markerPath, marker.ToJsonString());

        var status = _viet.GetStatus(_game);
        var assemblyVersion = typeof(ViethoaInstaller).Assembly.GetName().Version!;

        Assert.Equal($"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Build}", status.Version);
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
    public async Task RootPaksCustomPriorityMod_IsReportedButOfficialPakchunkIsIgnored()
    {
        var custom = Path.Combine(_paks, "CoolCharacter_P.pak");
        File.WriteAllText(custom, "MOD");
        File.WriteAllText(Path.Combine(_paks, "pakchunk0-WindowsNoEditor_P.pak"), "OFFICIAL");

        var conflicts = _viet.FindConflicts(_game);

        Assert.Contains(conflicts, x => x.Contains("CoolCharacter_P.pak", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(conflicts, x => x.Contains("pakchunk0", StringComparison.OrdinalIgnoreCase));
        var install = await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true);
        Assert.False(install.Success);
        Assert.Equal("MOD", File.ReadAllText(custom));
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
    public async Task ForeignPriority100Pak_IsReportedAndNeverDeleted()
    {
        Assert.True((await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true)).Success);
        var foreignPak = Path.Combine(Mods, "OtherMod_100_P.pak");
        var foreignSig = Path.ChangeExtension(foreignPak, ".sig");
        File.WriteAllText(foreignPak, "FOREIGN_MOD");
        File.WriteAllText(foreignSig, "FOREIGN_SIG");

        var conflicts = _viet.FindConflicts(_game);
        Assert.Contains(conflicts, x => x.Contains("OtherMod_100_P.pak", StringComparison.OrdinalIgnoreCase));

        var uninstall = await _viet.UninstallAsync(_game);
        Assert.True(uninstall.Success, uninstall.Error);
        Assert.Equal("FOREIGN_MOD", File.ReadAllText(foreignPak));
        Assert.Equal("FOREIGN_SIG", File.ReadAllText(foreignSig));
        Assert.False(File.Exists(Path.Combine(Mods, "WuWaVH_99_P.pak")));
    }

    [Fact]
    public async Task Uninstall_RefusesWhenTranslationPakWasOverwrittenByAnotherMod()
    {
        Assert.True((await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true)).Success);
        var managedPak = Path.Combine(Mods, "WuWaVH_99_P.pak");
        File.WriteAllText(managedPak, "OTHER_MOD_OVERWRITE");

        Assert.Contains(_viet.FindConflicts(_game), x => x.Contains("ghi đè", StringComparison.OrdinalIgnoreCase));
        var uninstall = await _viet.UninstallAsync(_game);

        Assert.False(uninstall.Success);
        Assert.Contains("không gỡ", uninstall.Error!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("OTHER_MOD_OVERWRITE", File.ReadAllText(managedPak));
    }

    [Fact]
    public async Task Uninstall_Force_RemovesOverwrittenManagedFileAfterExplicitChoice()
    {
        Assert.True((await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true)).Success);
        var managedPak = Path.Combine(Mods, "WuWaVH_99_P.pak");
        File.WriteAllText(managedPak, "OTHER_MOD_OVERWRITE");

        var uninstall = await _viet.UninstallAsync(_game, forceRemoveChangedFiles: true);

        Assert.True(uninstall.Success, uninstall.Error);
        Assert.False(File.Exists(managedPak));
        Assert.False(_viet.GetStatus(_game).Installed);
    }

    [Fact]
    public async Task Uninstall_RefusesWhenVersionLoaderWasOverwrittenByAnotherMod()
    {
        Assert.True((await _viet.InstallAsync(_game, NameVariant.English, withFont: false)).Success);
        var version = Path.Combine(_win64, "version.dll");
        File.WriteAllText(version, "OTHER_PROXY_LOADER");

        Assert.Contains(_viet.FindConflicts(_game), x => x.Contains("version.dll", StringComparison.OrdinalIgnoreCase));
        var uninstall = await _viet.UninstallAsync(_game);

        Assert.False(uninstall.Success);
        Assert.Equal("OTHER_PROXY_LOADER", File.ReadAllText(version));
        Assert.True(File.Exists(Path.Combine(Mods, "WuWaVH_99_P.pak")));
    }

    [Fact]
    public async Task KnownWinHttpLocalizationBundle_IsDetectedAndQuarantinedWithoutTouchingVhwFiles()
    {
        Assert.True((await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true)).Success);
        var foreignDir = Path.Combine(_win64, "wuwaVietHoa");
        Directory.CreateDirectory(foreignDir);
        var foreignTranslation = Path.Combine(foreignDir, "WuWaVH_99_P.pak");
        var foreignFont = Path.Combine(foreignDir, "Default_font_99_P.pak");
        var winhttp = Path.Combine(_win64, "winhttp.dll");
        File.WriteAllText(foreignTranslation, "FOREIGN_TRANSLATION");
        File.WriteAllText(foreignFont, "FOREIGN_FONT");
        File.WriteAllText(winhttp, "FOREIGN_PROXY");

        var conflicts = _viet.FindConflicts(_game);
        Assert.Contains(conflicts, x => x.Contains("winhttp.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(conflicts, x => x.Contains("wuwaVietHoa", StringComparison.OrdinalIgnoreCase));

        var result = _viet.QuarantineConflicts(_game);

        Assert.True(result.Success, result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.MovedFiles.Count);
        Assert.False(File.Exists(winhttp));
        Assert.False(Directory.Exists(foreignDir));
        Assert.True(File.Exists(Path.Combine(Mods, "WuWaVH_99_P.pak")));
        Assert.True(File.Exists(Path.Combine(Mods, "WahuFont_100_P.pak")));
        Assert.True(File.Exists(Path.Combine(result.Value.QuarantineDirectory, "quarantine-manifest.json")));
        Assert.Empty(_viet.FindConflicts(_game));
    }

    [Fact]
    public async Task DeleteConflicts_PermanentlyRemovesForeignBundleAndChangedManagedPak()
    {
        Assert.True((await _viet.InstallAsync(_game, NameVariant.HanViet, withFont: true)).Success);
        var managedPak = Path.Combine(Mods, "WuWaVH_99_P.pak");
        File.WriteAllText(managedPak, "OTHER_MOD_OVERWRITE");
        var foreignDir = Path.Combine(_win64, "wuwaVietHoa");
        Directory.CreateDirectory(foreignDir);
        var foreignTranslation = Path.Combine(foreignDir, "WuWaVH_99_P.pak");
        var foreignFont = Path.Combine(foreignDir, "Default_font_99_P.pak");
        var winhttp = Path.Combine(_win64, "winhttp.dll");
        File.WriteAllText(foreignTranslation, "FOREIGN_TRANSLATION");
        File.WriteAllText(foreignFont, "FOREIGN_FONT");
        File.WriteAllText(winhttp, "FOREIGN_PROXY");

        var result = _viet.DeleteConflicts(_game);

        Assert.True(result.Success, result.Error);
        Assert.Equal(4, result.Value);
        Assert.False(File.Exists(managedPak));
        Assert.False(File.Exists(winhttp));
        Assert.False(Directory.Exists(foreignDir));
        Assert.True(File.Exists(Path.Combine(Mods, "WahuFont_100_P.pak")));
        Assert.True(File.Exists(Path.Combine(_win64, "version.dll")));
        Assert.Empty(_viet.FindConflicts(_game));

        var uninstall = await _viet.UninstallAsync(_game);
        Assert.True(uninstall.Success, uninstall.Error);
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
