using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class GameLaunchServiceTests
{
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
}
