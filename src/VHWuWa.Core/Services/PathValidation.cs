namespace VHWuWa.Core.Services;

/// <summary>Kiểm tra an toàn đường dẫn: chống path traversal, đường dẫn tuyệt đối, thoát khỏi thư mục gốc.</summary>
public static class PathValidation
{
    private static readonly char[] WindowsInvalidNameChars = ['<', '>', ':', '"', '|', '?', '*'];
    private static readonly HashSet<string> WindowsReservedNames = new(
        new[]
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        },
        StringComparer.OrdinalIgnoreCase);

    /// <summary>True nếu candidate nằm trong rootDir (hoặc chính là rootDir).</summary>
    public static bool IsSubPathOf(string candidate, string rootDir)
    {
        var rootFull = Path.GetFullPath(rootDir)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var candidateFull = Path.GetFullPath(candidate)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return string.Equals(candidateFull, rootFull, StringComparison.OrdinalIgnoreCase)
               || candidateFull.StartsWith(rootFull + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True nếu <paramref name="relative"/> là đường dẫn tương đối an toàn (không ../, không tuyệt đối, không ký tự lạ).</summary>
    public static bool IsSafeRelativePath(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative)) return false;
        // Chuẩn hóa dấu phân cách
        var norm = relative.Replace('\\', '/').Trim();
        if (norm.StartsWith('/')) return false;                 // tuyệt đối kiểu unix
        if (norm.Length >= 2 && norm[1] == ':') return false;   // C:\ ...
        if (norm.StartsWith("//") || norm.StartsWith("\\\\")) return false; // UNC
        if (norm.Contains('\0')) return false;
        foreach (var seg in norm.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (seg == "..") return false;
            if (seg == ".") return false;
            if (seg.Trim() != seg) return false; // khoảng trắng đầu/cuối segment -> nghi ngờ
            if (seg.EndsWith('.')) return false; // Windows bỏ dấu chấm cuối tên
            if (seg.IndexOfAny(WindowsInvalidNameChars) >= 0) return false;
            var baseName = seg.Split('.')[0];
            if (WindowsReservedNames.Contains(baseName)) return false;
        }
        if (Path.IsPathRooted(relative)) return false;
        return true;
    }

    /// <summary>Ghép và kiểm chứng đích nằm TRONG rootDir. Ném UnauthorizedAccessException nếu thoát ra ngoài.</summary>
    public static string ResolveInsideRoot(string rootDir, string relative)
    {
        if (!IsSafeRelativePath(relative))
            throw new UnauthorizedAccessException($"Đường dẫn không an toàn: {relative}");

        var rootFull = Path.GetFullPath(rootDir);
        var combined = Path.GetFullPath(Path.Combine(rootFull, relative));

        var rootWithSep = rootFull.EndsWith(Path.DirectorySeparatorChar)
            ? rootFull
            : rootFull + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(combined, rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Đường dẫn thoát khỏi thư mục game: {relative}");
        }
        return combined;
    }
}
