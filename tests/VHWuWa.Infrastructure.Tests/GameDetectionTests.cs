using VHWuWa.Core.Models;
using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class GameDetectionTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "VHWuWa_Detect_" + Guid.NewGuid().ToString("N"));
    private readonly string _config;
    private readonly LogService _log;

    public GameDetectionTests()
    {
        _config = Path.Combine(_work, "Config");
        Directory.CreateDirectory(_config);
        File.WriteAllText(Path.Combine(_config, "game.json"), VhwJson.Serialize(new GameConfig
        {
            GameId = "wuthering-waves",
            GameName = "Wuthering Waves",
            Executable = "Client/Binaries/Win64/Client-Win64-Shipping.exe",
            RequiredFiles = { "Client/Binaries/Win64/Client-Win64-Shipping.exe" }
        }));
        _log = new LogService(new SettingsService(Path.Combine(_work, "appdata")));
    }

    [Fact]
    public void NormalizeGamePath_AcceptsParentClientPaksAndExecutable()
    {
        var parent = Path.Combine(_work, "Kuro Games");
        var game = Path.Combine(parent, "Wuthering Waves", "Wuthering Waves Game");
        var exe = Path.Combine(game, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
        File.WriteAllText(exe, "exe");
        var paks = Path.Combine(game, "Client", "Content", "Paks");
        Directory.CreateDirectory(paks);
        var detect = new GameDetectionService(_log, _config, new[] { _work });

        Assert.Equal(game, detect.NormalizeGamePath(parent));
        Assert.Equal(game, detect.NormalizeGamePath(Path.Combine(game, "Client")));
        Assert.Equal(game, detect.NormalizeGamePath(paks));
        Assert.Equal(game, detect.NormalizeGamePath(exe));
    }

    [Fact]
    public void AutoDetect_FindsSingularGameFolderAndRequiresRealClientExecutable()
    {
        var expected = Path.Combine(_work, "Game", "Wuthering Waves Game");
        var exe = Path.Combine(expected, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
        File.WriteAllText(exe, "exe");

        var fake = Path.Combine(_work, "Games", "Wuthering Waves Game", "Client");
        Directory.CreateDirectory(fake);
        var detect = new GameDetectionService(_log, _config, new[] { _work });

        var found = detect.AutoDetect();

        Assert.Single(found);
        Assert.Equal(expected, found[0]);
        Assert.DoesNotContain(Path.GetDirectoryName(fake)!, found);
    }

    public void Dispose()
    {
        _log.Dispose();
        try { if (Directory.Exists(_work)) Directory.Delete(_work, recursive: true); } catch { }
    }
}
