using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class R29MountedPakTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "WahuR29FailClosed_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void MissingPackage_DirectInstallIsStrictlyNonMutating()
    {
        Directory.CreateDirectory(_root);
        var app = Path.Combine(_root, "App");
        var game = Path.Combine(_root, "Game");
        Directory.CreateDirectory(app);
        Directory.CreateDirectory(game);

        Assert.False(R29MountedPak.PackagePresent(app));
        var result = R29MountedPak.Install(app, game);

        Assert.False(result.Success);
        Assert.False(Directory.Exists(Path.Combine(game, "Client", "Saved", "Resources", "3.8.0", "Mount")));
        Assert.False(Directory.Exists(Path.Combine(game, "Client", "Saved", "Resources", "3.8.0", "Vietnamese")));
        Assert.Empty(Directory.GetFileSystemEntries(game));
    }

    [Fact]
    public void UnknownExternalMount_IsNeverAcceptedAsManagedR29()
    {
        var game = Path.Combine(_root, "Game");
        var dir = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0", "Mount");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "Vietnamese.txt"), "::Mount::\nexternal\n::Del::\n");
        Assert.False(R29MountedPak.IsInstalled(game));
        Assert.False(R29MountedPak.Uninstall(game).Success);
        Assert.True(File.Exists(Path.Combine(dir, "Vietnamese.txt")));
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

        Assert.NotNull(R29MountedPak.StockSig(game));

        Directory.Delete(manifests, recursive: true);
        Assert.NotNull(R29MountedPak.StockSig(game));

        File.Delete(signature);
        // Không có SIG game thì không được tự bịa placeholder rồi báo tương thích.
        Assert.Null(R29MountedPak.StockSig(game));
        File.WriteAllBytes(signature, new byte[] { 1, 2, 3 });
        Assert.NotNull(R29MountedPak.StockSig(game));

        File.Delete(officialMount);
        // MountLang_en.txt is not modified by this installer and can be absent.
        Assert.NotNull(R29MountedPak.StockSig(game));
        Directory.Delete(Path.GetDirectoryName(officialMount)!);
        // Missing destination Mount directory is now created automatically.
        Assert.NotNull(R29MountedPak.StockSig(game));
    }

    [Fact]
    public void AnyExistingLanguageSignatureVersion_CanProvideTheMountSignature()
    {
        var (game, signature, _) = CreateMountReadyGame();
        Assert.NotNull(R29MountedPak.StockSig(game));

        // Updates may leave a newer language SIG without the old 3.8.6 folder.
        File.Delete(signature);
        var newer = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0",
            "Lang_en", "3.9.1", "pakchunk10-WindowsNoEditor_P.sig");
        Directory.CreateDirectory(Path.GetDirectoryName(newer)!);
        File.WriteAllBytes(newer, new byte[] { 3, 9, 1 });
        Assert.NotNull(R29MountedPak.StockSig(game));

        File.Delete(newer);
        Assert.Null(R29MountedPak.StockSig(game));
    }

    [Fact]
    public void BaseAndUnversionedEnglishSignatures_AreRecognized()
    {
        var (game, signature, _) = CreateMountReadyGame();
        File.Delete(signature);
        Assert.Null(R29MountedPak.StockSig(game));

        var lang = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0", "Lang_en");
        var baseSig = Path.Combine(lang, "Base", "pakchunk10-WindowsNoEditor_P.sig");
        Directory.CreateDirectory(Path.GetDirectoryName(baseSig)!);
        File.WriteAllBytes(baseSig, new byte[] { 1, 2, 3 });
        Assert.NotNull(R29MountedPak.StockSig(game));

        File.Delete(baseSig);
        var rootSig = Path.Combine(lang, "pakchunk10-WindowsNoEditor_P.sig");
        File.WriteAllBytes(rootSig, new byte[] { 1, 2, 3 });
        Assert.NotNull(R29MountedPak.StockSig(game));
        File.WriteAllBytes(rootSig, Array.Empty<byte>());
        Assert.Null(R29MountedPak.StockSig(game));
    }

    [Fact]
    public void MissingResourceMountAndSig_FailClosedBeforeWritingFiles()
    {
        var (game, _, _) = CreateMountReadyGame();
        var resource = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0");
        Directory.Delete(resource, recursive: true);

        Assert.Null(R29MountedPak.StockSig(game));

        Directory.CreateDirectory(resource);
        Assert.Null(R29MountedPak.StockSig(game));
        Assert.False(File.Exists(Path.Combine(resource, "Vietnamese", "Vietnamese_99_P.sig")));
    }

    [Fact]
    public void SteamLanguageSig_WithOfficialMountSha1_IsAcceptedWithoutEnglish()
    {
        var (game, english, _) = CreateMountReadyGame();
        File.Delete(english);
        var resource = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0");
        var jaSig = Path.Combine(resource, "Lang_ja", "3.8.11", "pakchunk11-WindowsNoEditor_P.sig");
        Directory.CreateDirectory(Path.GetDirectoryName(jaSig)!);
        File.WriteAllBytes(jaSig, [5, 10, 22, 18]);
        var sha1 = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(File.ReadAllBytes(jaSig)));
        var jaMount = Path.Combine(resource, "Mount", "MountLang_ja.txt");
        var mountText = "::Mount::\r\nLang_ja/3.8.11/pakchunk11-WindowsNoEditor_P,11,PAKSHA,"
            + sha1 + ",,\r\n::Del::\r\n";
        File.WriteAllText(jaMount, mountText);

        Assert.NotNull(R29MountedPak.StockSig(game));
        Assert.Equal(mountText, File.ReadAllText(jaMount));
        Assert.Equal([5, 10, 22, 18], File.ReadAllBytes(jaSig));
    }

    [Theory]
    [InlineData("missing-mount")]
    [InlineData("wrong-sha")]
    [InlineData("wrong-path")]
    [InlineData("zero-sig")]
    [InlineData("foreign-unreferenced")]
    public void UnverifiedLanguageSig_CannotBeUsedForR29(string failure)
    {
        var (game, english, _) = CreateMountReadyGame();
        File.Delete(english);
        var resource = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0");
        var jaSig = Path.Combine(resource, "Lang_ja", "3.8.11", "pakchunk11-WindowsNoEditor_P.sig");
        Directory.CreateDirectory(Path.GetDirectoryName(jaSig)!);
        File.WriteAllBytes(jaSig, failure == "zero-sig" ? Array.Empty<byte>() : [6, 7, 8]);
        var sha1 = Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(File.ReadAllBytes(jaSig)));
        var jaMount = Path.Combine(resource, "Mount", "MountLang_ja.txt");
        var mountPath = failure == "wrong-path" ? "Lang_ja/3.8.11/RoleVoice_99_P"
            : "Lang_ja/3.8.11/pakchunk11-WindowsNoEditor_P";
        if (failure == "foreign-unreferenced")
        {
            // Signature present in the language directory is not proof of ownership.
            File.WriteAllText(jaMount, "::Mount::\r\n::Del::\r\n");
        }
        else if (failure != "missing-mount")
        {
            File.WriteAllText(jaMount, "::Mount::\r\n" + mountPath + ",11,PAKSHA,"
                + (failure == "wrong-sha" ? new string('A', 40) : sha1) + ",,\r\n::Del::\r\n");
        }

        Assert.Null(R29MountedPak.StockSig(game));
        Assert.True(File.Exists(jaSig));
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

    [Theory]
    [InlineData("3.0.18.0", false)]
    [InlineData("3.0.19.0", true)]
    [InlineData("3.0.20.0", true)]
    [InlineData("3.0.21.0", true)]
    [InlineData("3.0.22.0", true)]
    [InlineData("3.2.0.0", true)]
    [InlineData("3.1.0.0", true)]
    public void ReleaseRequired_PersistsAcrossFutureR29Versions(string version, bool expected)
    {
        Assert.Equal(expected, R29MountedPak.VersionRequiresR29(Version.Parse(version)));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
    }
}
