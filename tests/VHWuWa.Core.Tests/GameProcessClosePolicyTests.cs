using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public sealed class GameProcessClosePolicyTests
{
    private const string GameRoot = @"D:\Games\Wuthering Waves\Wuthering Waves Game";

    [Fact]
    public void ClientWithinSelectedGameRootCanBeClosed()
    {
        var full = Path.Combine(GameRoot, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
        Assert.True(GameProcessClosePolicy.IsSelectedGameProcess(GameRoot, "Client-Win64-Shipping", full));
        Assert.True(GameProcessClosePolicy.IsSelectedGameProcess(GameRoot, "Client-Win64-Shipping",
            full.ToUpperInvariant()));
    }

    [Fact]
    public void GameFromAnotherInstallationIsNeverMatched()
    {
        var other = @"E:\OtherGame\Client/Binaries/Win64/Client-Win64-Shipping.exe";
        Assert.False(GameProcessClosePolicy.IsSelectedGameProcess(GameRoot, "Client-Win64-Shipping", other));
    }

    [Fact]
    public void ExactLegacyGameWrapperCanMatchButLauncherAndSteamNeverMatch()
    {
        Assert.True(GameProcessClosePolicy.IsSelectedGameProcess(GameRoot, "Wuthering Waves",
            Path.Combine(GameRoot, "Wuthering Waves.exe")));
        Assert.False(GameProcessClosePolicy.IsSelectedGameProcess(GameRoot, "launcher",
            Path.Combine(GameRoot, "launcher.exe")));
        Assert.False(GameProcessClosePolicy.IsSelectedGameProcess(GameRoot, "steam",
            @"C:\Program Files (x86)\Steam\steam.exe"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("launcher.exe")]
    [InlineData(@"E:\AnotherGame\Client/Binaries/Win64/Client-Win64-Shipping.exe")]
    public void InvalidOrUnrelatedPathsAreRejected(string path)
    {
        Assert.False(GameProcessClosePolicy.IsSelectedGameProcess(GameRoot, "Client-Win64-Shipping", path));
    }

    [Fact]
    public void MissingGameRootNeverAuthorizesProcessStop()
    {
        Assert.False(GameProcessClosePolicy.IsSelectedGameProcess("", "Client-Win64-Shipping",
            @"D:\Games\Client\Binaries\Win64\Client-Win64-Shipping.exe"));
    }
}