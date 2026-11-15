using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class R29MountedPakTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "WahuR29FailClosed_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void MissingPackageAndGame_FailClosedWithNoWrites()
    {
        Directory.CreateDirectory(_root);
        var app = Path.Combine(_root, "App");
        var game = Path.Combine(_root, "Game");
        Directory.CreateDirectory(app);
        Directory.CreateDirectory(game);

        Assert.False(R29MountedPak.PackagePresent(app));
        Assert.False(R29MountedPak.PackageVerified(app));
        Assert.False(R29MountedPak.GameCompatible(game));
        Assert.False(R29MountedPak.IsInstalled(game));
        Assert.False(R29MountedPak.Install(app, game).Success);
        Assert.False(Directory.Exists(Path.Combine(game, "Client", "Saved")));
    }

    [Fact]
    public void UnknownExternalMount_IsNeverAcceptedAsManagedR29()
    {
        var game = Path.Combine(_root, "Game");
        var dir = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0", "Mount");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "wahuviethoa.txt"), "::Mount::\nexternal\n::Del::\n");
        Assert.False(R29MountedPak.IsInstalled(game));
        Assert.False(R29MountedPak.Uninstall(game).Success);
        Assert.True(File.Exists(Path.Combine(dir, "wahuviethoa.txt")));
    }

    [Fact]
    public void R29ReleaseMarker_KeepsMissingPakFailClosed()
    {
        var app = Path.Combine(_root, "R29App");
        var content = Path.Combine(app, "content");
        Directory.CreateDirectory(content);
        File.WriteAllText(Path.Combine(content, "r29-release.json"),
            "{\"appVersion\":\"3.0.19\",\"translationRelease\":\"R29\"}");

        Assert.True(R29MountedPak.ReleaseRequired(app));
        Assert.False(R29MountedPak.PackagePresent(app));
        Assert.False(R29MountedPak.PackageVerified(app));
    }

    [Fact]
    public void ChangedGameExeAndNewerResourceManifest_DoNotBlockR29Mount()
    {
        // Neither the EXE hash pinned on MAY-007 nor the exact resource manifest
        // is a requirement for installing a text PAK using the same Mount layout.
        var (game, signature, officialMount) = CreateMountReadyGame();
        var manifests = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0", "ResManifest");
        Directory.CreateDirectory(manifests);
        File.WriteAllText(Path.Combine(manifests, "ManifestResource_3.8.12.txt"), "new game resource");

        Assert.True(R29MountedPak.GameCompatible(game));

        Directory.Delete(manifests, recursive: true);
        Assert.True(R29MountedPak.GameCompatible(game));

        File.Delete(signature);
        Assert.False(R29MountedPak.GameCompatible(game));
        File.WriteAllBytes(signature, new byte[] { 1, 2, 3 });
        Assert.True(R29MountedPak.GameCompatible(game));

        File.Delete(officialMount);
        // MountLang_en.txt is not modified by this installer and can be absent.
        Assert.True(R29MountedPak.GameCompatible(game));
        Directory.Delete(Path.GetDirectoryName(officialMount)!);
        // Missing destination Mount directory is now created automatically.
        Assert.True(R29MountedPak.GameCompatible(game));
    }

    [Fact]
    public void AnyExistingLanguageSignatureVersion_CanProvideTheMountSignature()
    {
        var (game, signature, _) = CreateMountReadyGame();
        Assert.True(R29MountedPak.GameCompatible(game));

        // Updates may leave a newer language SIG without the old 3.8.6 folder.
        File.Delete(signature);
        var newer = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0",
            "Lang_en", "3.9.1", "pakchunk10-WindowsNoEditor_P.sig");
        Directory.CreateDirectory(Path.GetDirectoryName(newer)!);
        File.WriteAllBytes(newer, new byte[] { 3, 9, 1 });
        Assert.True(R29MountedPak.GameCompatible(game));

        File.Delete(newer);
        Assert.False(R29MountedPak.GameCompatible(game));
    }

    [Fact]
    public void BaseAndUnversionedEnglishSignatures_AreRecognized()
    {
        var (game, signature, _) = CreateMountReadyGame();
        File.Delete(signature);
        Assert.False(R29MountedPak.GameCompatible(game));

        var lang = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0", "Lang_en");
        var baseSig = Path.Combine(lang, "Base", "pakchunk10-WindowsNoEditor_P.sig");
        Directory.CreateDirectory(Path.GetDirectoryName(baseSig)!);
        File.WriteAllBytes(baseSig, new byte[] { 1, 2, 3 });
        Assert.True(R29MountedPak.GameCompatible(game));
        Assert.Null(R29MountedPak.CompatibilityIssue(game));

        File.Delete(baseSig);
        var rootSig = Path.Combine(lang, "pakchunk10-WindowsNoEditor_P.sig");
        File.WriteAllBytes(rootSig, new byte[] { 1, 2, 3 });
        Assert.True(R29MountedPak.GameCompatible(game));
        File.WriteAllBytes(rootSig, Array.Empty<byte>());
        Assert.False(R29MountedPak.GameCompatible(game));
        Assert.Contains("SIG", R29MountedPak.CompatibilityIssue(game));
    }

    [Fact]
    public void MissingResourceAndMount_GiveSpecificPreflightErrors()
    {
        var (game, _, mountFile) = CreateMountReadyGame();
        var resource = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0");
        Directory.Delete(resource, recursive: true);
        Assert.Contains("3.8.0", R29MountedPak.CompatibilityIssue(game));

        Directory.CreateDirectory(resource);
        Assert.Contains("SIG", R29MountedPak.CompatibilityIssue(game));
    }

    private (string Game, string Signature, string OfficialMount) CreateMountReadyGame()
    {
        var game = Path.Combine(_root, Guid.NewGuid().ToString("N"));
        var exe = Path.Combine(game, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
        File.WriteAllText(exe, "different EXE build from MAY-007");

        var resource = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0");
        var mount = Path.Combine(resource, "Mount", "MountLang_en.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(mount)!);
        File.WriteAllText(mount, "::Mount::\r\n");

        var sig = Path.Combine(resource, "Lang_en", "3.8.9", "pakchunk10-WindowsNoEditor_P.sig");
        Directory.CreateDirectory(Path.GetDirectoryName(sig)!);
        File.WriteAllBytes(sig, new byte[] { 1, 2, 3 });
        return (game, sig, mount);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
    }
}
