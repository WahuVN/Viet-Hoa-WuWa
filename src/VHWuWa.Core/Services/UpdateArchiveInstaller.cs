using System.IO.Compression;

namespace VHWuWa.Core.Services;

/// <summary>
/// Đọc được cả ZIP chỉ chứa file app và ZIP bộ cài đầy đủ dạng
/// VHWuWa_BanCai/app/*. Chỉ phần ứng dụng được chép vào thư mục app hiện tại.
/// </summary>
public static class UpdateArchiveInstaller
{
    public const string MainExecutable = "VHWuWa.exe";
    public const string UpdaterExecutable = "VHWuWa.Updater.exe";
    private const int MaxArchiveEntries = 20_000;
    private const long MaxUncompressedBytes = 4L * 1024 * 1024 * 1024;

    public static string DetectApplicationPrefix(ZipArchive archive)
    {
        if (archive.Entries.Count == 0)
            throw new InvalidDataException("ZIP cập nhật rỗng.");
        if (archive.Entries.Count > MaxArchiveEntries)
            throw new InvalidDataException($"ZIP cập nhật có quá nhiều tệp ({archive.Entries.Count:N0}).");

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalLength = 0;

        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName.Replace('\\', '/');
            if (!PathValidation.IsSafeRelativePath(name) || name.Contains(':'))
                throw new InvalidDataException("ZIP cập nhật chứa đường dẫn không an toàn: " + name);

            if (!name.EndsWith('/') && !paths.Add(name))
                throw new InvalidDataException("ZIP cập nhật chứa đường dẫn trùng lặp: " + name);

            try
            {
                totalLength = checked(totalLength + entry.Length);
            }
            catch (OverflowException)
            {
                throw new InvalidDataException("Kích thước ZIP cập nhật không hợp lệ.");
            }
            if (totalLength > MaxUncompressedBytes)
                throw new InvalidDataException("ZIP cập nhật vượt giới hạn 4 GB sau giải nén.");

            var separator = name.LastIndexOf('/');
            var fileName = separator >= 0 ? name[(separator + 1)..] : name;
            if (fileName.Equals(MainExecutable, StringComparison.OrdinalIgnoreCase))
                candidates.Add(separator >= 0 ? name[..(separator + 1)] : string.Empty);
        }

        if (candidates.Count != 1)
            throw new InvalidDataException(
                candidates.Count == 0
                    ? "ZIP cập nhật không chứa VHWuWa.exe."
                    : "ZIP cập nhật chứa nhiều thư mục ứng dụng VHWuWa không rõ ràng.");

        var prefix = candidates.Single();
        if (prefix.Length == 0 || prefix.EndsWith("app/", StringComparison.OrdinalIgnoreCase))
        {
            if (!paths.Contains(prefix + UpdaterExecutable))
                throw new InvalidDataException(
                    "ZIP cập nhật thiếu VHWuWa.Updater.exe trong cùng thư mục với ứng dụng.");
            return prefix;
        }

        throw new InvalidDataException(
            "VHWuWa.exe phải nằm ở gốc ZIP cập nhật hoặc trong thư mục app chuẩn.");
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
