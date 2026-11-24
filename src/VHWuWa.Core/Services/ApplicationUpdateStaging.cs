using System.IO;

namespace VHWuWa.Core.Services;

/// <summary>
/// App update staging must not depend on a globally cleaned TEMP folder.
/// The relauncher accepts only dedicated application staging or legacy one-level TEMP paths.
/// </summary>
public static class ApplicationUpdateStaging
{
    private const string Prefix = "VHWuWa_Update_";

    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "VHWuWa", "UpdateStaging");

    public static string NewDirectoryPath() =>
        Path.Combine(Root, Prefix + Guid.NewGuid().ToString("N"));

    public static string CreateDirectory()
    {
        var path = NewDirectoryPath();
        Directory.CreateDirectory(path);
        return path;
    }

    public static string? ValidateCleanupDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var cleanup = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parent = Path.GetDirectoryName(cleanup);
        var name = Path.GetFileName(cleanup);

        var stableRoot = Path.GetFullPath(Root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var legacyTemp = Path.GetFullPath(Path.GetTempPath())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var isDedicated = string.Equals(parent, stableRoot, StringComparison.OrdinalIgnoreCase)
            && name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            && Guid.TryParseExact(name[Prefix.Length..], "N", out _);
        var isLegacyTemp = string.Equals(parent, legacyTemp, StringComparison.OrdinalIgnoreCase)
            && name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);

        if (!isDedicated && !isLegacyTemp)
            throw new InvalidDataException("Thư mục dọn cập nhật không hợp lệ.");

        if (Directory.Exists(cleanup) &&
            (File.GetAttributes(cleanup) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("Không được dọn thư mục cập nhật là liên kết.");

        return cleanup;
    }
}