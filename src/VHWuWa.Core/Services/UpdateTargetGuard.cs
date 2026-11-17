using System.IO;

namespace VHWuWa.Core.Services;

/// <summary>
/// Fail-closed update guard for broad/shared folders. The app must live inside its own
/// dedicated directory, never directly in Desktop, Downloads, Documents or a profile root.
/// No writes are permitted before this guard passes.
/// </summary>
public static class UpdateTargetGuard
{
    public static void EnsureSafeTarget(string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
            throw new InvalidDataException("Thu muc cap nhat rong.");

        var fullTarget = Path.GetFullPath(targetDirectory);
        var target = Canonical(fullTarget);
        var root = Path.GetPathRoot(fullTarget);
        if (string.IsNullOrWhiteSpace(root) ||
            string.Equals(Canonical(root), target, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Khong duoc cap nhat tai goc o dia.");

        var forbidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static void Add(HashSet<string> set, string? dir)
        {
            if (!string.IsNullOrWhiteSpace(dir))
                set.Add(Canonical(dir));
        }
        Add(forbidden, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
        Add(forbidden, Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));
        Add(forbidden, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        Add(forbidden, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        Add(forbidden, Environment.GetFolderPath(Environment.SpecialFolder.Windows));
        Add(forbidden, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        Add(forbidden, Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        Add(forbidden, Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        Add(forbidden, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Add(forbidden, Path.Combine(profile, "Downloads"));
        Add(forbidden, Path.Combine(profile, "Desktop"));
        Add(forbidden, Path.Combine(profile, "Documents"));
        var oneDrive = Environment.GetEnvironmentVariable("OneDrive");
        var oneDriveConsumer = Environment.GetEnvironmentVariable("OneDriveConsumer");
        foreach (var drive in new[] { oneDrive, oneDriveConsumer })
        {
            if (string.IsNullOrWhiteSpace(drive)) continue;
            Add(forbidden, Path.Combine(drive, "Desktop"));
            Add(forbidden, Path.Combine(drive, "Documents"));
            Add(forbidden, Path.Combine(drive, "Downloads"));
        }

        if (forbidden.Contains(target))
            throw new InvalidDataException(
                "Tu choi cap nhat ca thu muc he thong/Desktop/Downloads. " +
                "Hay cai VHWuWa.exe vao mot thu muc ung dung rieng.");

        // A relocated Desktop may be named something else; KnownFolder paths above cover it.
        // A directory named Desktop is never a safe app root even when it is relocated.
        var name = Path.GetFileName(target);
        if (name.Equals("Desktop", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Downloads", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Documents", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Khong duoc cap nhat vao thu muc dung chung: " + name);

        // Reject links to a shared folder (or any reparse point on the target path).
        // Only inspect existing segments; never create/delete directories during validation.
        var cursor = target;
        while (!string.IsNullOrEmpty(cursor))
        {
            if (Directory.Exists(cursor) &&
                (File.GetAttributes(cursor) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Khong cap nhat qua junction/symlink: " + cursor);
            var parent = Path.GetDirectoryName(cursor);
            if (string.IsNullOrEmpty(parent) || string.Equals(parent, cursor, StringComparison.OrdinalIgnoreCase))
                break;
            cursor = parent;
        }
    }

    private static string Canonical(string path)
    {
        var full = Path.GetFullPath(path);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
