using System.IO.Compression;

namespace VHWuWa.Core.Services;

/// <summary>
/// Đọc được cả ZIP chỉ chứa file app và ZIP bộ cài đầy đủ dạng
/// VHWuWa_BanCai/app/*. Chỉ phần ứng dụng được chép vào thư mục app hiện tại.
/// </summary>
public static class UpdateArchiveInstaller
{
    public const string MainExecutable = "VHWuWa.exe";

    public static string DetectApplicationPrefix(ZipArchive archive)
    {
        var candidates = archive.Entries
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .Where(name => PathValidation.IsSafeRelativePath(name)
                           && name.EndsWith(MainExecutable, StringComparison.OrdinalIgnoreCase))
            .Select(name => name[..^MainExecutable.Length])
            .ToList();

        if (candidates.Any(prefix => prefix.Length == 0))
            return "";

        // Gói người chơi có .../app/VHWuWa.exe. Ưu tiên đúng thư mục app,
        // không lấy nhầm một bản EXE nằm trong tài liệu hoặc thư mục phụ khác.
        var appPrefix = candidates
            .Where(prefix => prefix.EndsWith("app/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(prefix => prefix.Length)
            .FirstOrDefault();
        if (appPrefix != null)
            return appPrefix;

        if (candidates.Count == 1)
            return candidates[0];

        throw new InvalidDataException("Không tìm thấy thư mục ứng dụng VHWuWa hợp lệ trong ZIP cập nhật.");
    }

    public static void ExtractApplication(string zipPath, string targetDirectory)
    {
        var root = Path.GetFullPath(targetDirectory);
        using var archive = ZipFile.OpenRead(zipPath);
        var prefix = DetectApplicationPrefix(archive);
        var extractedMainExecutable = false;

        foreach (var entry in archive.Entries)
        {
            var fullName = entry.FullName.Replace('\\', '/');
            if (!PathValidation.IsSafeRelativePath(fullName)
                || !fullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var relativeName = fullName[prefix.Length..];
            if (string.IsNullOrWhiteSpace(relativeName) || relativeName.EndsWith('/'))
                continue;
            if (!PathValidation.IsSafeRelativePath(relativeName))
                continue;

            var destination = PathValidation.ResolveInsideRoot(root, relativeName);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, overwrite: true);
            if (relativeName.Equals(MainExecutable, StringComparison.OrdinalIgnoreCase))
                extractedMainExecutable = true;
        }

        if (!extractedMainExecutable || !File.Exists(Path.Combine(root, MainExecutable)))
            throw new InvalidDataException("ZIP cập nhật không tạo được VHWuWa.exe ở đúng thư mục ứng dụng.");
    }
}
