using System.Reflection;
using VHWuWa.Core.Models;
using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class R29FontMountedPakTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(),
        "vhw_r29font_" + Guid.NewGuid().ToString("N"));
    private readonly string _game;
    private readonly string _resources;
    private readonly string _stockPak;
    private readonly string _officialMount;

    public R29FontMountedPakTests()
    {
        _game = Path.Combine(_root, "Wuthering Waves Game");
        _resources = Path.Combine(_game, "Client", "Saved", "Resources", "3.8.0");
        var win64 = Path.Combine(_game, "Client", "Binaries", "Win64");
        var lang = Path.Combine(_resources, "Lang_en", "Base");
        var mount = Path.Combine(_resources, "Mount");
        var stockDir = Path.Combine(_resources, "Option_Pack_LY_UHD", "Base");
        Directory.CreateDirectory(win64);
        Directory.CreateDirectory(lang);
        Directory.CreateDirectory(mount);
        Directory.CreateDirectory(stockDir);
        File.WriteAllText(Path.Combine(win64, "Client-Win64-Shipping.exe"), "MOCK_EXE");
        File.WriteAllText(Path.Combine(lang, "pakchunk10-WindowsNoEditor_P.sig"), "STOCK_SIGNATURE");
        _stockPak = Path.Combine(stockDir, "pakchunk54-UHD-WindowsNoEditor.pak");
        File.WriteAllText(_stockPak, "IMMUTABLE_STOCK_ASSET");
        _officialMount = Path.Combine(mount, "MountLang_en.txt");
        File.WriteAllText(_officialMount, "IMMUTABLE_OFFICIAL_LANGUAGE_MOUNT");
        File.WriteAllText(Path.Combine(mount, "MountOption_Pack_LY_UHD.txt"),
            "::Mount::\nOption_Pack_LY_UHD/Base/pakchunk54-UHD-WindowsNoEditor,5,SHA,SHA,,\n::Del::\n");
        File.WriteAllText(Path.Combine(mount, "MountLauncher.txt"),
            "::Mount::\nLauncher/Base/pakchunk1-WindowsNoEditor_P,1,SHA,SHA,,\n::Del::\n");
    }

    private static string RealFont(string name)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12 && directory is not null; i++, directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Fonts", name);
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("Test fixture font PAK missing from source repo", name);
    }

    private Result InstallFixture(string fontPak)
    {
        var method = typeof(R29FontMountedPak).GetMethod("InstallCore",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (Result)method!.Invoke(null, new object[] { _game, fontPak })!;
    }

    [Fact]
    public void FontMount_InstallsAndChangesFontWithoutTouchingStockOrTranslation()
    {
        var original = File.ReadAllText(_officialMount);
        var stock = File.ReadAllText(_stockPak);
        var first = InstallFixture(RealFont("Verdana_100_P.pak"));
        Assert.True(first.Success, first.Error);
        Assert.True(R29FontMountedPak.IsInstalled(_game));
        Assert.Equal("Verdana_100_P.pak", R29FontMountedPak.CurrentFontPak(_game));

        var next = InstallFixture(RealFont("Arial_100_P.pak"));
        Assert.True(next.Success, next.Error);
        Assert.Equal("Arial_100_P.pak", R29FontMountedPak.CurrentFontPak(_game));
        var sidecar = Path.Combine(_resources, "Mount", "wahufont.txt");
        Assert.Contains("wahufont/WahuFont_100_P,100,", File.ReadAllText(sidecar));
        Assert.Equal(stock, File.ReadAllText(_stockPak));
        Assert.Equal(original, File.ReadAllText(_officialMount));
        Assert.True(File.Exists(Path.Combine(_resources, "Mount", "MountLauncher.txt")));

        var remove = R29FontMountedPak.Remove(_game);
        Assert.True(remove.Success, remove.Error);
        Assert.False(R29FontMountedPak.IsInstalled(_game));
        Assert.True(File.Exists(_stockPak));
        Assert.Equal(original, File.ReadAllText(_officialMount));
    }

    [Fact]
    public void InvalidFontFailsButValidInstallOverwritesTamperedOwnedTarget()
    {
        Assert.True(InstallFixture(RealFont("Verdana_100_P.pak")).Success);
        var ownPak = Path.Combine(_resources, "wahufont", "WahuFont_100_P.pak");
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            File.ReadAllBytes(ownPak)));
        var invalid = Path.Combine(_root, "Fake_100_P.pak");
        File.WriteAllText(invalid, "not a V12 PAK");
        var failed = InstallFixture(invalid);
        Assert.False(failed.Success);
        Assert.Equal(hash, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            File.ReadAllBytes(ownPak))));
        Assert.Equal("Verdana_100_P.pak", R29FontMountedPak.CurrentFontPak(_game));

        var originalSidecar = Path.Combine(_resources, "Mount", "wahufont.txt");
        File.AppendAllText(originalSidecar, "TAMPER");
        var repaired = InstallFixture(RealFont("Arial_100_P.pak"));
        Assert.True(repaired.Success, repaired.Error);
        Assert.DoesNotContain("TAMPER", File.ReadAllText(originalSidecar));
        Assert.Equal("Arial_100_P.pak", R29FontMountedPak.CurrentFontPak(_game));
        Assert.True(File.Exists(_stockPak));
    }

    [Fact]
    public void ExactWahuFontTarget_IsOverwrittenWithoutTouchingUnrelatedStockFiles()
    {
        var foreign = Path.Combine(_resources, "Mount", "wahufont.txt");
        File.WriteAllText(foreign, "::Mount::\nFOREIGN\n");
        var result = InstallFixture(RealFont("Verdana_100_P.pak"));
        Assert.True(result.Success, result.Error);
        Assert.Contains("wahufont/WahuFont_100_P,100,", File.ReadAllText(foreign));
        Assert.True(File.Exists(Path.Combine(_resources, "wahufont", "WahuFont_100_P.pak")));
        Assert.True(File.Exists(_stockPak));
        Assert.Equal("IMMUTABLE_OFFICIAL_LANGUAGE_MOUNT", File.ReadAllText(_officialMount));
    }

    [Fact]
    public void AllBundledFontPaksAreValidV12()
    {
        var fontDirectory = Path.GetDirectoryName(RealFont("Verdana_100_P.pak"))!;
        var packs = Directory.GetFiles(fontDirectory, "*_100_P.pak");
        Assert.Equal(73, packs.Length);
        foreach (var pak in packs)
            Assert.True(PakV12Converter.TryVerifyV12(pak, out var issue),
                Path.GetFileName(pak) + ": " + issue);
    }


    [Fact]
    public async Task LexendCustomFont_RepakV11ToV12_CanMountSafely()
    {
        var font = RealFont("Lexend-660.ttf");
        var directory = new DirectoryInfo(Path.GetDirectoryName(font)!);
        var repak = Path.Combine(directory.Parent!.FullName, "tools", "repak.exe");
        Assert.True(File.Exists(repak), "Bundled repak.exe not found");
        var stage = Path.Combine(_root, "font-content");
        var stagingFont = Path.Combine(stage, "Client", "Content", "Aki", "UI",
            "Framework", "LGUI", "Font");
        Directory.CreateDirectory(stagingFont);
        File.Copy(font, Path.Combine(stagingFont, "LaguSansBold.ufont"));
        var v11 = Path.Combine(_root, "Lexend_100_P.v11.pak");
        var v12 = Path.Combine(_root, "Lexend_100_P.pak");
        using var proc = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = repak,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                ArgumentList = {
                    "pack", "--version", "V11", "--mount-point", "../../../",
                    stage, v11
                }
            }
        };
        Assert.True(proc.Start());
        await proc.WaitForExitAsync();
        var output = await proc.StandardError.ReadToEndAsync();
        Assert.True(proc.ExitCode == 0 && File.Exists(v11), output);
        PakV12Converter.ConvertV11ToV12(v11, v12);
        Assert.True(PakV12Converter.TryVerifyV12(v12, out var error), error);
        var result = InstallFixture(v12);
        Assert.True(result.Success, result.Error);
        Assert.Equal("Lexend_100_P.pak", R29FontMountedPak.CurrentFontPak(_game));
        Assert.Equal("IMMUTABLE_STOCK_ASSET", File.ReadAllText(_stockPak));
    }

    [Fact]
    public void RestoreDefaultWithoutCustomFont_IsIdempotent()
    {
        var result = R29FontMountedPak.Remove(_game);
        Assert.True(result.Success, result.Error);
        Assert.True(File.Exists(_officialMount));
        Assert.True(File.Exists(_stockPak));
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }
}
