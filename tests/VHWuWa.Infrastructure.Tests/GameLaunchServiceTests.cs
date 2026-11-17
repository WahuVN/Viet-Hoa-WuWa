using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class GameLaunchServiceTests
{
    [Fact]
    public void GetOfficialLauncherPath_PrefersRealKuroLauncherInParentFolder()
    {
        var temp = Path.Combine(Path.GetTempPath(), "vhwuwa-launch-" + Guid.NewGuid().ToString("N"));
        var installRoot = Path.Combine(temp, "Wuthering Waves");
        var gameRoot = Path.Combine(installRoot, "Wuthering Waves Game");
        Directory.CreateDirectory(gameRoot);
        var launcher = Path.Combine(installRoot, "launcher.exe");
        File.WriteAllBytes(launcher, Array.Empty<byte>());

        try
        {
            var service = new GameLaunchService(new TestLogService());
            Assert.Equal(launcher, service.GetOfficialLauncherPath(gameRoot));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void GetOfficialLauncherPath_FallsBackToLegacyGameWrapper()
    {
        var temp = Path.Combine(Path.GetTempPath(), "vhwuwa-launch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var service = new GameLaunchService(new TestLogService());
            Assert.Equal(Path.Combine(temp, "Wuthering Waves.exe"), service.GetOfficialLauncherPath(temp));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }
    [Fact]
    public void GetSteamLaunchUri_UsesMatchingSteamManifestInsteadOfGuessingAppId()
    {
        var temp = Path.Combine(Path.GetTempPath(), "vhwuwa-steam-uri-" + Guid.NewGuid().ToString("N"));
        var steamapps = Path.Combine(temp, "SteamLibrary", "steamapps");
        var game = Path.Combine(steamapps, "common", "Wuthering Waves", "Wuthering Waves Game");
        var exe = Path.Combine(game, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
        File.WriteAllBytes(exe, new byte[] { 1 });

        try
        {
            var manifest = Path.Combine(steamapps, "appmanifest_1234567.acf");
            File.WriteAllText(manifest, "\"AppState\"\n{\n\t\"appid\"\t\"1234567\"\n\t\"installdir\"\t\"Wuthering Waves\"\n}");
            Assert.Equal("steam://rungameid/1234567", GameLaunchService.GetSteamLaunchUri(game));
            Assert.Equal("steam://rungameid/1234567",
                GameLaunchService.GetSteamLaunchUri(Path.Combine(game, "Client")));

            File.WriteAllText(manifest, "\"AppState\"\n{\n\t\"installdir\"\t\"Other Game\"\n}");
            Assert.Null(GameLaunchService.GetSteamLaunchUri(game));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void GetSteamLaunchUri_KuroNonSteamFolderHasNoSteamOverride()
    {
        var temp = Path.Combine(Path.GetTempPath(), "vhwuwa-kuro-path-" + Guid.NewGuid().ToString("N"));
        var game = Path.Combine(temp, "Kuro Games", "Wuthering Waves", "Wuthering Waves Game");
        Directory.CreateDirectory(game);
        try
        {
            Assert.Null(GameLaunchService.GetSteamLaunchUri(game));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void CreateStartInfo_ForceCSharp_AddsOnlyExpectedArgument()
    {
        var exe = Path.Combine("D:\\Game Folder", "Client-Win64-Shipping.exe");

        var info = GameLaunchService.CreateStartInfo(exe, forceCSharpEnvironment: true);

        Assert.Equal(exe, info.FileName);
        Assert.Equal(Path.GetDirectoryName(exe), info.WorkingDirectory);
        Assert.True(info.UseShellExecute);
        Assert.Equal("runas", info.Verb);
        Assert.Equal(new[] { "-ForceEnableCSharpEnvironment" }, info.ArgumentList);
    }

    [Fact]
    public void CreateStartInfo_NormalMode_HasNoArguments()
    {
        var info = GameLaunchService.CreateStartInfo(
            Path.Combine("D:\\Game", "Client-Win64-Shipping.exe"), forceCSharpEnvironment: false);

        Assert.Empty(info.ArgumentList);
    }

    [Fact]
    public void CreateWatchdogStartInfo_UsesElevatedSelfHelperAndGamePath()
    {
        var helper = Path.Combine("D:\\Tools", "VHWuWa.exe");
        var game = Path.Combine("D:\\Game Folder", "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");

        var info = GameLaunchService.CreateWatchdogStartInfo(helper, game, forceCSharpEnvironment: false);

        Assert.Equal(helper, info.FileName);
        Assert.Equal(Path.GetDirectoryName(helper), info.WorkingDirectory);
        Assert.True(info.UseShellExecute);
        Assert.Equal("runas", info.Verb);
        Assert.Equal(new[]
        {
            "--game-exit-watchdog", "--game-exe", game, "--force-csharp", "false"
        }, info.ArgumentList);
    }

    [Fact]
    public void CreateWatchdogStartInfo_ForwardsCSharpMode()
    {
        var info = GameLaunchService.CreateWatchdogStartInfo(
            "D:\\Tools\\VHWuWa.exe",
            "D:\\Game\\Client-Win64-Shipping.exe",
            forceCSharpEnvironment: true);

        Assert.Equal("true", info.ArgumentList[^1]);
    }

    private sealed class TestLogService : ILogService
    {
        public string LogDirectory => Path.GetTempPath();
        public void Info(string operation, string message) { }
        public void Warn(string operation, string message) { }
        public void Error(string operation, string message, Exception? ex = null) { }
        public IReadOnlyList<LogEntry> ReadRecent(int max = 500, string? levelFilter = null,
            string? search = null) => Array.Empty<LogEntry>();
        public void Clear() { }
    }
}
