using System.Text.Json;
using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public sealed class StagedPakInspectorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(),
        "VHWuWa_StagedPak_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void Inspect_NoManifest_ReportsMissingWithoutInstallation()
    {
        var info = StagedPakInspector.Inspect(_root);
        Assert.False(info.Present);
        Assert.False(info.CanInstallToGame);
    }

    [Fact]
    public void Inspect_RejectsIncorrectContentPathBeforeReadingPak()
    {
        WriteManifest(pakPath: "../../outside.pak");
        var info = StagedPakInspector.Inspect(_root);
        Assert.True(info.Present);
        Assert.False(info.ShaVerified);
        Assert.False(info.CanInstallToGame);
    }

    [Fact]
    public void Inspect_RejectsManifestRequestingAutoInstall()
    {
        WriteManifest(autoInstall: true);
        var info = StagedPakInspector.Inspect(_root);
        Assert.False(info.ShaVerified);
        Assert.False(info.CanInstallToGame);
    }

    [Fact]
    public void Inspect_RejectsIncorrectHashAndMissingPak()
    {
        WriteManifest();
        var info = StagedPakInspector.Inspect(_root);
        Assert.True(info.Present);
        Assert.False(info.ShaVerified);
        Assert.Contains("thiếu", info.Message, StringComparison.OrdinalIgnoreCase);
    }

    private void WriteManifest(string? pakPath = null, bool autoInstall = false)
    {
        var dir = Path.Combine(_root, "content", "3.8.10");
        Directory.CreateDirectory(dir);
        var manifest = new Dictionary<string, object>
        {
            ["schema"] = "WAHU_PC_R28_PREUPDATE_NOT_INSTALLABLE_UNTIL_LOADER_VERIFIED",
            ["game_version"] = "3.8.1",
            ["resource_version"] = "3.8.10",
            ["content_release"] = "R28",
            ["pak_path"] = pakPath ?? "content/3.8.10/" + StagedPakInspector.FileName,
            ["pak_sha256"] = StagedPakInspector.ExpectedSha256,
            ["pak_entries"] = 62,
            ["automatic_install_enabled"] = autoInstall,
            ["loader_3_8_verified"] = false
        };
        File.WriteAllText(Path.Combine(dir, "R28_PREUPDATE_MANIFEST.json"),
            JsonSerializer.Serialize(manifest));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
