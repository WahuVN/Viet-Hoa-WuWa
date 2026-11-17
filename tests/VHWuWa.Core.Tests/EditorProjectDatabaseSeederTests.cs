using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public sealed class EditorProjectDatabaseSeederTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "WahuEditorSeed_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void FreshInstall_SeedsFromDefaultOnlyOnce()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "project.default.db"), "default");
        Assert.True(EditorProjectDatabaseSeeder.EnsureSeeded(_dir));
        Assert.Equal("default", File.ReadAllText(Path.Combine(_dir, "project.db")));
        File.WriteAllText(Path.Combine(_dir, "project.db"), "user modified");
        Assert.False(EditorProjectDatabaseSeeder.EnsureSeeded(_dir));
        Assert.Equal("user modified", File.ReadAllText(Path.Combine(_dir, "project.db")));
    }

    [Fact]
    public void ExistingProjectSurvivesEvenWhenDefaultIsAbsent()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "project.db"), "precious data");
        Assert.False(EditorProjectDatabaseSeeder.EnsureSeeded(_dir));
        Assert.Equal("precious data", File.ReadAllText(Path.Combine(_dir, "project.db")));
    }

    [Fact]
    public void FreshInstallWithoutDefaultFailsClosed()
    {
        Directory.CreateDirectory(_dir);
        Assert.Throws<FileNotFoundException>(() =>
            EditorProjectDatabaseSeeder.EnsureSeeded(_dir));
        Assert.False(File.Exists(Path.Combine(_dir, "project.db")));
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { }
    }
}
