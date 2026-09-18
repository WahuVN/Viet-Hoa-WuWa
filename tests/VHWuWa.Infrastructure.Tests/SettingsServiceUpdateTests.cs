using VHWuWa.Core.Models;
using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class SettingsServiceUpdateTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "VHWuWa_SettingsUpdate_" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void AutoCheckUpdate_DefaultsToTrue_OnFirstRun()
    {
        var settings = new SettingsService(_root);

        Assert.True(settings.Settings.AutoCheckUpdate);
    }

    [Fact]
    public void AutoCheckUpdate_False_PersistsAcrossRestart()
    {
        var first = new SettingsService(_root);
        first.Settings.AutoCheckUpdate = false;
        first.Save();

        var second = new SettingsService(_root);

        Assert.False(second.Settings.AutoCheckUpdate);
    }

    [Fact]
    public void AutoCheckUpdate_True_CanBeRestoredAndPersists()
    {
        var first = new SettingsService(_root);
        first.Settings.AutoCheckUpdate = false;
        first.Save();

        var second = new SettingsService(_root);
        second.Settings.AutoCheckUpdate = true;
        second.Save();

        var third = new SettingsService(_root);

        Assert.True(third.Settings.AutoCheckUpdate);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch { }
    }
}
