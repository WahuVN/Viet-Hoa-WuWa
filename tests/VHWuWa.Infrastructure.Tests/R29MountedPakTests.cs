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

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
    }
}
