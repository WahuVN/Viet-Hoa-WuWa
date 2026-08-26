using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Security;
using System.Text.RegularExpressions;

namespace VHWuWa.Core.Services;

/// <summary>
/// Chuyển đổi và chuẩn hóa tất cả thông báo lỗi kỹ thuật của .NET và Windows sang tiếng Việt rõ ràng,
/// kèm theo hướng dẫn khắc phục cụ thể cho người dùng.
/// </summary>
public static class ErrorFormatter
{
    private static readonly Regex PathRegex = new Regex(@"'([^']+)'|""([^""]+)""", RegexOptions.Compiled);

    /// <summary>
    /// Chuẩn hóa chuỗi lỗi và ngoại lệ sang tiếng Việt dễ hiểu.
    /// </summary>
    public static string Humanize(string? message, Exception? ex = null, string? prefix = null)
    {
        var rawMessage = message?.Trim() ?? string.Empty;
        var exMessage = ex?.Message?.Trim() ?? string.Empty;

        // Tách tiền tố nội bộ nếu có dạng "Tiền tố: Chi tiết lỗi" (không tách khi colon nằm trong đường dẫn hoặc sau dấu nháy)
        string? extractedPrefix = null;
        var firstQuote = rawMessage.IndexOfAny(new[] { '\'', '"' });
        var colonIdx = rawMessage.IndexOf(':');
        if (colonIdx > 0 && colonIdx < 40 && (firstQuote == -1 || colonIdx < firstQuote))
        {
            var candidate = rawMessage[..colonIdx].Trim();
            if (candidate.Length > 1 && !candidate.Equals("http", StringComparison.OrdinalIgnoreCase) && !candidate.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                extractedPrefix = candidate;
                rawMessage = rawMessage[(colonIdx + 1)..].Trim();
            }
        }

        // Trích xuất đường dẫn file / folder nếu có trong thông báo
        var targetPath = ExtractPath(rawMessage);
        if (string.IsNullOrEmpty(targetPath))
            targetPath = ExtractPath(exMessage);

        var pathDisplay = !string.IsNullOrEmpty(targetPath) ? " '" + targetPath + "'" : string.Empty;

        string vietnameseDetail;

        // 1. Kiểm tra theo Type của Exception trước
        if (ex is UnauthorizedAccessException or SecurityException ||
            IsAccessDenied(rawMessage) || IsAccessDenied(exMessage))
        {
            vietnameseDetail = "Không có quyền truy cập hoặc ghi file vào đường dẫn" + pathDisplay + ".\n" +
                               "👉 Hướng dẫn khắc phục: Vui lòng tắt tool, nhấp chuột phải vào biểu tượng Tool và chọn 'Run as administrator' (Chạy với tư cách quản trị viên). " +
                               "Nếu game cài tại 'C:\\Program Files', hãy đảm bảo thư mục game có quyền ghi (Write Permission).";
        }
        else if (ex is FileNotFoundException || IsFileNotFound(rawMessage) || IsFileNotFound(exMessage))
        {
            vietnameseDetail = "Không tìm thấy tệp tin" + pathDisplay + ".\n" +
                               "👉 Hướng dẫn: Tệp tin có thể đã bị xóa hoặc bị phần mềm diệt virus (Windows Defender/Antivirus) chặn. Vui lòng kiểm tra lại.";
        }
        else if (ex is DirectoryNotFoundException || IsDirectoryNotFound(rawMessage) || IsDirectoryNotFound(exMessage))
        {
            vietnameseDetail = "Không tìm thấy thư mục" + pathDisplay + ".\n" +
                               "👉 Hướng dẫn: Vui lòng kiểm tra lại đường dẫn cài đặt game trong tab Cài đặt.";
        }
        else if (IsFileLocked(rawMessage) || IsFileLocked(exMessage))
        {
            vietnameseDetail = "Tệp tin" + pathDisplay + " đang bị khóa do một chương trình khác (như Game Wuthering Waves hoặc Launcher) đang sử dụng.\n" +
                               "👉 Hướng dẫn khắc phục: Hãy đóng hoàn toàn Game và Launcher (kiểm tra trong Task Manager xem còn Client-Win64-Shipping.exe hay không), sau đó thử lại.";
        }
        else if (IsDiskFull(rawMessage) || IsDiskFull(exMessage))
        {
            vietnameseDetail = "Ổ đĩa lưu trữ đã hết dung lượng trống.\n" +
                               "👉 Hướng dẫn khắc phục: Vui lòng dọn dẹp và giải phóng thêm dung lượng trên ổ đĩa rồi thực hiện lại.";
        }
        else if (ex is HttpRequestException or SocketException || IsNetworkError(rawMessage) || IsNetworkError(exMessage))
        {
            vietnameseDetail = "Lỗi kết nối mạng hoặc không thể tải dữ liệu từ máy chủ (GitHub/Server).\n" +
                               "👉 Hướng dẫn khắc phục: Vui lòng kiểm tra kết nối Internet, tắt VPN/Proxy (nếu có) hoặc thử lại sau ít phút.";
        }
        else if (ex is TaskCanceledException or OperationCanceledException or TimeoutException ||
                 IsTimeout(rawMessage) || IsTimeout(exMessage))
        {
            vietnameseDetail = "Thao tác đã bị hủy bỏ hoặc quá thời gian chờ (Timeout). Vui lòng thử lại.";
        }
        else if (ex is Win32Exception win32Ex)
        {
            vietnameseDetail = win32Ex.NativeErrorCode switch
            {
                5 => "Bị từ chối quyền truy cập hệ thống (Win32 Access Denied)" + pathDisplay + ".\n👉 Hãy mở Tool bằng quyền Quản trị viên (Run as administrator).",
                2 => "Hệ thống không tìm thấy file thực thi được yêu cầu" + pathDisplay + ".",
                1223 => "Thao tác đã bị hủy bỏ bởi người dùng.",
                _ => "Lỗi hệ thống Windows (" + win32Ex.NativeErrorCode + "): " + win32Ex.Message
            };
        }
        else
        {
            // Làm sạch các chuỗi tiền tố tiếng Việt thô sơ
            var clean = !string.IsNullOrEmpty(rawMessage) ? rawMessage : exMessage;
            vietnameseDetail = CleanRawMessage(clean);
        }

        // Ưu tiên prefix truyền vào, nếu không thì dùng prefix tách từ chuỗi gốc
        var finalPrefix = !string.IsNullOrEmpty(prefix) ? prefix : extractedPrefix;
        if (!string.IsNullOrEmpty(finalPrefix))
        {
            var cleanPrefix = CleanPrefix(finalPrefix);
            return cleanPrefix + ": " + vietnameseDetail;
        }

        return CleanPrefix(vietnameseDetail);
    }

    private static string CleanPrefix(string text)
    {
        return text
            .Replace("Cài lỗi:", "Cài đặt thất bại:")
            .Replace("Gỡ lỗi:", "Gỡ cài đặt thất bại:")
            .Replace("Cài lỗi", "Cài đặt thất bại")
            .Replace("Gỡ lỗi", "Gỡ cài đặt thất bại");
    }

    private static string CleanRawMessage(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg))
            return "Đã xảy ra lỗi không xác định. Vui lòng thử lại.";

        return CleanPrefix(msg);
    }

    private static string? ExtractPath(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var match = PathRegex.Match(text);
        if (match.Success)
        {
            return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        }

        // Tìm đường dẫn dạng C:\...
        var winPathMatch = Regex.Match(text, @"[A-Za-z]:\\[^:\*\?""<>\|\r\n]+");
        if (winPathMatch.Success)
        {
            return winPathMatch.Value.TrimEnd('.', ',', ' ', '\'', '"');
        }

        return null;
    }

    private static bool IsAccessDenied(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var lower = text.ToLowerInvariant();
        return lower.Contains("access to the path") && lower.Contains("is denied")
               || lower.Contains("access is denied")
               || lower.Contains("permission denied")
               || lower.Contains("unauthorizedaccess")
               || lower.Contains("hresult -2147024891")
               || lower.Contains("0x80070005");
    }

    private static bool IsFileNotFound(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var lower = text.ToLowerInvariant();
        return lower.Contains("could not find file")
               || lower.Contains("the system cannot find the file specified")
               || lower.Contains("filenotfoundexception")
               || lower.Contains("hresult -2147024894")
               || lower.Contains("0x80070002");
    }

    private static bool IsDirectoryNotFound(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var lower = text.ToLowerInvariant();
        return lower.Contains("could not find a part of the path")
               || lower.Contains("the system cannot find the path specified")
               || lower.Contains("directorynotfoundexception")
               || lower.Contains("hresult -2147024893")
               || lower.Contains("0x80070003");
    }

    private static bool IsFileLocked(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var lower = text.ToLowerInvariant();
        return lower.Contains("being used by another process")
               || lower.Contains("used by another process")
               || lower.Contains("the process cannot access the file")
               || lower.Contains("sharing violation")
               || lower.Contains("hresult -2147024864")
               || lower.Contains("0x80070020");
    }

    private static bool IsDiskFull(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var lower = text.ToLowerInvariant();
        return lower.Contains("there is not enough space on the disk")
               || lower.Contains("not enough disk space")
               || lower.Contains("disk is full")
               || lower.Contains("hresult -2147024784")
               || lower.Contains("0x80070070");
    }

    private static bool IsNetworkError(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var lower = text.ToLowerInvariant();
        return lower.Contains("no such host is known")
               || lower.Contains("a connection attempt failed")
               || lower.Contains("the remote name could not be resolved")
               || lower.Contains("the ssl connection could not be established")
               || lower.Contains("connection refused")
               || lower.Contains("network is unreachable")
               || lower.Contains("httprequestexception")
               || lower.Contains("socketexception");
    }

    private static bool IsTimeout(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        var lower = text.ToLowerInvariant();
        return lower.Contains("the operation has timed out")
               || lower.Contains("the request was canceled due to the configured httpclient.timeout")
               || lower.Contains("taskcanceledexception")
               || lower.Contains("timeoutexception");
    }
}
