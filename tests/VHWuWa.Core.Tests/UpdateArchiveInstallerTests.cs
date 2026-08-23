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
            ["VHWuWa.Updater.next.exe"] = "new-updater",
        });
        var target = Path.Combine(_root, "app-only");

        UpdateArchiveInstaller.ExtractApplication(zip, target);

        Assert.Equal("new-app", File.ReadAllText(Path.Combine(target, "VHWuWa.exe")));
        Assert.Equal("new-updater", File.ReadAllText(Path.Combine(target, "VHWuWa.Updater.next.exe")));
    }

    [Fact]
    public void AppOnlyPackage_DoesNotOverwriteRunningLegacyUpdater()
    {
        var zip = CreateZip(new Dictionary<string, string>
        {
            ["VHWuWa.exe"] = "new-app",
            ["VHWuWa.Updater.next.exe"] = "new-updater",
        });
        var target = Path.Combine(_root, "legacy-app");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "VHWuWa.Updater.exe"), "legacy-updater");

        // Tương đương cách updater 2.0.0 giải nén trực tiếp vào appDir.
        ZipFile.ExtractToDirectory(zip, target, overwriteFiles: true);

        Assert.Equal("new-app", File.ReadAllText(Path.Combine(target, "VHWuWa.exe")));
        Assert.Equal("legacy-updater", File.ReadAllText(Path.Combine(target, "VHWuWa.Updater.exe")));
        Assert.Equal("new-updater", File.ReadAllText(Path.Combine(target, "VHWuWa.Updater.next.exe")));
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
