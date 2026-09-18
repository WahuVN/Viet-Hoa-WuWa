using System.IO.Compression;
using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public sealed class UpdateArchiveInstallerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "VHWuWa_UpdateTests_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void ExtractApplication_HandlesFullPlayerPackageWithoutNestingFolders()
    {
        var zip = CreateZip(new Dictionary<string, string>
        {
            ["VHWuWa_BanCai/Chay VHWuWa.bat"] = "launcher",
            ["VHWuWa_BanCai/app/VHWuWa.exe"] = "new-app",
            ["VHWuWa_BanCai/app/VHWuWa.Updater.exe"] = "new-updater",
            ["VHWuWa_BanCai/app/Config/game.json"] = "{}",
        });
        var target = Path.Combine(_root, "installed", "app");

        UpdateArchiveInstaller.ExtractApplication(zip, target);

        Assert.Equal("new-app", File.ReadAllText(Path.Combine(target, "VHWuWa.exe")));
        Assert.Equal("new-updater", File.ReadAllText(Path.Combine(target, "VHWuWa.Updater.exe")));
        Assert.True(File.Exists(Path.Combine(target, "Config", "game.json")));
        Assert.False(Directory.Exists(Path.Combine(target, "VHWuWa_BanCai")));
        Assert.False(File.Exists(Path.Combine(target, "Chay VHWuWa.bat")));
    }

    [Fact]
    public void ExtractApplication_HandlesAppOnlyPackage()
    {
        var zip = CreateZip(new Dictionary<string, string>
        {
            ["VHWuWa.exe"] = "new-app",
            ["VHWuWa.Updater.exe"] = "new-updater",
        });
        var target = Path.Combine(_root, "app-only");

        UpdateArchiveInstaller.ExtractApplication(zip, target);

        Assert.Equal("new-app", File.ReadAllText(Path.Combine(target, "VHWuWa.exe")));
        Assert.Equal("new-updater", File.ReadAllText(Path.Combine(target, "VHWuWa.Updater.exe")));
    }

    [Fact]
    public void ExtractApplication_RejectsAmbiguousApplicationRoots()
    {
        var zip = CreateZip(new Dictionary<string, string>
        {
            ["VHWuWa.exe"] = "new-app",
            ["nested/app/VHWuWa.exe"] = "other-app",
        });

        Assert.Throws<InvalidDataException>(() =>
            UpdateArchiveInstaller.ExtractApplication(zip, Path.Combine(_root, "ambiguous")));
    }

    [Theory]
    [InlineData("../escape.txt")]
    [InlineData("app/../../escape.txt")]
    [InlineData("app/file.txt:evil")]
    [InlineData("app/CON.txt")]
    [InlineData("app/name./file.txt")]
    public void ExtractApplication_RejectsUnsafeWindowsPaths(string unsafePath)
    {
        var zip = CreateZip(new Dictionary<string, string>
        {
            ["app/VHWuWa.exe"] = "new-app",
            [unsafePath] = "unsafe",
        });

        Assert.Throws<InvalidDataException>(() =>
            UpdateArchiveInstaller.ExtractApplication(zip, Path.Combine(_root, "unsafe")));
    }

    [Fact]
    public void ExtractApplication_RejectsArchiveWithoutMainExecutable()
    {
        var zip = CreateZip(new Dictionary<string, string> { ["readme.txt"] = "no app" });

        Assert.Throws<InvalidDataException>(() =>
            UpdateArchiveInstaller.ExtractApplication(zip, Path.Combine(_root, "invalid")));
    }

    private string CreateZip(IReadOnlyDictionary<string, string> files)
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".zip");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, contents) in files)
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(contents);
        }
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
