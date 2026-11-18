using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public class ApplicationUpdateStagingTests
{
    [Fact]
    public void CreatesDedicatedPathOutsideGlobalTempAndValidatesIt()
    {
        var path = ApplicationUpdateStaging.CreateDirectory();
        try
        {
            Assert.True(Directory.Exists(path));
            Assert.Equal(
                Path.GetFullPath(path),
                ApplicationUpdateStaging.ValidateCleanupDirectory(path));
            Assert.Equal(
                Path.GetFullPath(ApplicationUpdateStaging.Root),
                Path.GetDirectoryName(path));
        }
        finally
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
    }

    [Fact]
    public void AcceptsLegacyTempUpdateDirectory()
    {
        var oldPath = Path.Combine(
            Path.GetTempPath(), "VHWuWa_Update_" + Guid.NewGuid().ToString("N"));
        Assert.Equal(Path.GetFullPath(oldPath),
            ApplicationUpdateStaging.ValidateCleanupDirectory(oldPath));
    }

    [Fact]
    public void RejectsWrongFolderAndInvalidStagingName()
    {
        var unrelated = Path.Combine(
            ApplicationUpdateStaging.Root, "UserData_" + Guid.NewGuid().ToString("N"));
        Assert.Throws<InvalidDataException>(() =>
            ApplicationUpdateStaging.ValidateCleanupDirectory(unrelated));
        Assert.Throws<InvalidDataException>(() =>
            ApplicationUpdateStaging.ValidateCleanupDirectory(Path.GetTempPath()));
    }
}