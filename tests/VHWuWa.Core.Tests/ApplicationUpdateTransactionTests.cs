using System.IO.Compression;
using System.Security.Cryptography;
using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public sealed class ApplicationUpdateTransactionTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "VHWuWa_TransactionTests_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Rollback_RestoresExactOldTreeAndRemovesNewFiles()
    {
        var target = CreateInstalledApplication();
        var zip = CreateUpdateZip();
        var hash = Sha256(zip);

        using var transaction = ApplicationUpdateTransaction.Begin(
            zip, target, expectedVersion: null, expectedSha256: hash);

        Assert.Equal("new-app", File.ReadAllText(Path.Combine(target, "VHWuWa.exe")));
        Assert.Equal("user-setting", File.ReadAllText(Path.Combine(target, "Config", "user.json")));
        Assert.True(File.Exists(Path.Combine(target, "introduced-by-update.txt")));

        transaction.Rollback();

        Assert.Equal("old-app", File.ReadAllText(Path.Combine(target, "VHWuWa.exe")));
        Assert.Equal("old-updater", File.ReadAllText(Path.Combine(target, "VHWuWa.Updater.exe")));
        Assert.Equal("user-setting", File.ReadAllText(Path.Combine(target, "Config", "user.json")));
        Assert.False(File.Exists(Path.Combine(target, "introduced-by-update.txt")));
        Assert.False(Directory.Exists(transaction.BackupDirectory));
    }

    [Fact]
    public void Commit_KeepsNewAppAndPreservesLocalFiles()
    {
        var target = CreateInstalledApplication();
        var zip = CreateUpdateZip();

        using var transaction = ApplicationUpdateTransaction.Begin(
            zip, target, expectedVersion: null, expectedSha256: Sha256(zip));
        transaction.Commit();

        Assert.Equal("new-app", File.ReadAllText(Path.Combine(target, "VHWuWa.exe")));
        Assert.Equal("new-updater", File.ReadAllText(Path.Combine(target, "VHWuWa.Updater.exe")));
        Assert.Equal("user-setting", File.ReadAllText(Path.Combine(target, "Config", "user.json")));
        Assert.False(Directory.Exists(transaction.BackupDirectory));
    }

    [Fact]
    public void FailureAfterActivation_AutomaticallyRestoresOldTree()
    {
        var target = CreateInstalledApplication();
        var zip = CreateUpdateZip();

        var error = Assert.Throws<InvalidOperationException>(() =>
            ApplicationUpdateTransaction.Begin(
                zip,
                target,
                expectedVersion: null,
                expectedSha256: Sha256(zip),
                checkpoint: checkpoint =>
                {
                    if (checkpoint == UpdateTransactionCheckpoint.CandidateActivated)
                        throw new InvalidOperationException("simulated crash before health check");
                }));

        Assert.Contains("simulated crash", error.Message);
        Assert.Equal("old-app", File.ReadAllText(Path.Combine(target, "VHWuWa.exe")));
        Assert.Equal("old-updater", File.ReadAllText(Path.Combine(target, "VHWuWa.Updater.exe")));
        Assert.False(File.Exists(Path.Combine(target, "introduced-by-update.txt")));
    }

    [Fact]
    public void HashMismatch_DoesNotTouchInstalledApplication()
    {
        var target = CreateInstalledApplication();
        var zip = CreateUpdateZip();

        Assert.Throws<InvalidDataException>(() =>
            ApplicationUpdateTransaction.Begin(
                zip, target, expectedVersion: null, expectedSha256: new string('0', 64)));

        Assert.Equal("old-app", File.ReadAllText(Path.Combine(target, "VHWuWa.exe")));
        Assert.Equal("user-setting", File.ReadAllText(Path.Combine(target, "Config", "user.json")));
    }

    [Fact]
    public void MissingUpdaterInPackage_DoesNotTouchInstalledApplication()
    {
        var target = CreateInstalledApplication();
        var zip = CreateZip(new Dictionary<string, string>
        {
            ["VHWuWa.exe"] = "new-app",
        });

        Assert.Throws<InvalidDataException>(() =>
            ApplicationUpdateTransaction.Begin(
                zip, target, expectedVersion: null, expectedSha256: Sha256(zip)));

        Assert.Equal("old-app", File.ReadAllText(Path.Combine(target, "VHWuWa.exe")));
        Assert.Equal("old-updater", File.ReadAllText(Path.Combine(target, "VHWuWa.Updater.exe")));
    }

    private string CreateInstalledApplication()
    {
        var target = Path.Combine(_root, "installed", "app");
        Directory.CreateDirectory(Path.Combine(target, "Config"));
        File.WriteAllText(Path.Combine(target, "VHWuWa.exe"), "old-app");
        File.WriteAllText(Path.Combine(target, "VHWuWa.Updater.exe"), "old-updater");
        File.WriteAllText(Path.Combine(target, "Config", "user.json"), "user-setting");
        File.WriteAllText(Path.Combine(target, "old-only.txt"), "old-only");
        return target;
    }

    private string CreateUpdateZip() => CreateZip(new Dictionary<string, string>
    {
        ["VHWuWa.exe"] = "new-app",
        ["VHWuWa.Updater.exe"] = "new-updater",
        ["introduced-by-update.txt"] = "new-file",
    });

    private string CreateZip(IReadOnlyDictionary<string, string> files)
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, Guid.NewGuid().ToString("N") + ".zip");
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, content) in files)
        {
            var entry = archive.CreateEntry(name);
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
        return path;
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
