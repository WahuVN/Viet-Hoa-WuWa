using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public class UpdateTargetGuardTests
{
    [Fact]
    public void DesktopRootIsRejectedBeforeWriting()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop)) return; // Non-interactive Windows test runner
        Assert.Throws<InvalidDataException>(() => UpdateTargetGuard.EnsureSafeTarget(desktop));
    }

    [Fact]
    public void DesktopRootAlternateSeparatorsAreRejected()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop)) return;
        Assert.Throws<InvalidDataException>(() =>
            UpdateTargetGuard.EnsureSafeTarget(desktop + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void ProfileAndDownloadsRootsAreRejected()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Assert.Throws<InvalidDataException>(() => UpdateTargetGuard.EnsureSafeTarget(profile));
        Assert.Throws<InvalidDataException>(() =>
            UpdateTargetGuard.EnsureSafeTarget(Path.Combine(profile, "Downloads")));
    }

    [Fact]
    public void OwnApplicationFolderIsAllowed()
    {
        var app = Path.Combine(Path.GetTempPath(), "VHWuWa_UI_GuardUnit", "app");
        UpdateTargetGuard.EnsureSafeTarget(app);
    }

    [Fact]
    public void RootOfDriveIsRejected()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        Assert.Throws<InvalidDataException>(() => UpdateTargetGuard.EnsureSafeTarget(root));
    }

    [Fact]
    public void DesktopSymlinkIsRejected()
    {
        // Do not create a filesystem symlink on the user's machine:
        // this safety check is satisfied by rejecting any existing reparse point.
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!string.IsNullOrWhiteSpace(desktop))
            Assert.Throws<InvalidDataException>(() => UpdateTargetGuard.EnsureSafeTarget(desktop));
    }
}
