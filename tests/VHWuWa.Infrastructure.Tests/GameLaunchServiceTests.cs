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
}
