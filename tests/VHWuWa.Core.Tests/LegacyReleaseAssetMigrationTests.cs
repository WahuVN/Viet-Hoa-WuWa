using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public sealed class LegacyReleaseAssetMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "WahuLegacySafe_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void NoVerifiedR29_DoesNotCopyOrRemoveAnyOldContent()
    {
        var file = Path.Combine(_root, "App", "content", "WuWaVH_EN_99_P.pak");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "user file");
        var result = LegacyReleaseAssetMigration.BackupAndDeactivate(
            Path.Combine(_root, "App"), Path.Combine(_root, "Backups"), verifiedR29: false);
        Assert.Equal(0, result.BackedUp);
        Assert.True(File.Exists(file));
        Assert.False(Directory.Exists(Path.Combine(_root, "Backups")));
    }

    [Fact]
    public void UnknownOrModifiedSameNameFiles_ArePreserved()
    {
        var app = Path.Combine(_root, "App");
        var file = Path.Combine(app, "content", "WuWaVH_EN_99_P.pak");
        var foreign = Path.Combine(app, "content", "custom-user.pak");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "different from v3.0.18 public release");
        File.WriteAllText(foreign, "keep");
        var result = LegacyReleaseAssetMigration.BackupAndDeactivate(
            app, Path.Combine(_root, "Backups"), verifiedR29: true);
        Assert.Equal(0, result.BackedUp);
        Assert.Equal(1, result.UnknownPreserved);
        Assert.Empty(result.Errors);
        Assert.Equal("different from v3.0.18 public release", File.ReadAllText(file));
        Assert.Equal("keep", File.ReadAllText(foreign));
    }

    [Fact]
    public void BackupInsideApp_IsRejectedBeforeAnyModification()
    {
        var app = Path.Combine(_root, "App");
        var file = Path.Combine(app, "content", "WuWaVH_EN_99_P.pak");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "keep");
        Assert.Throws<InvalidDataException>(() =>
            LegacyReleaseAssetMigration.BackupAndDeactivate(
                app, Path.Combine(app, "Backups"), verifiedR29: true));
        Assert.True(File.Exists(file));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); }
        catch { }
    }
}
