using System;
using System.IO;
using System.Net.Http;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Core.Tests;

public class ErrorFormatterTests
{
    [Fact]
    public void Humanize_AccessDenied_TranslatesToVietnameseWithAdminInstruction()
    {
        var raw = @"Access to the path 'C:\Program Files\Wuthering Waves\Wuthering Waves Game\Client\Content\Paks\~WuWaMods' is denied.";
        var result = ErrorFormatter.Humanize(raw);

        Assert.Contains("Không có quyền truy cập hoặc ghi file", result);
        Assert.Contains("Run as administrator", result);
        Assert.Contains(@"C:\Program Files\Wuthering Waves", result);
    }

    [Fact]
    public void Humanize_UnauthorizedAccessException_TranslatesProperly()
    {
        var ex = new UnauthorizedAccessException(@"Access to the path 'D:\Game\Paks' is denied.");
        var result = ErrorFormatter.Humanize(null, ex, "Cài lỗi");

        Assert.StartsWith("Cài đặt thất bại:", result);
        Assert.Contains("Run as administrator", result);
    }

    [Fact]
    public void Humanize_FileLockedByAnotherProcess_TranslatesProperly()
    {
        var raw = @"The process cannot access the file 'D:\Game\Client-Win64-Shipping.exe' because it is being used by another process.";
        var result = ErrorFormatter.Humanize(raw);

        Assert.Contains("đang bị khóa", result);
        Assert.Contains("Task Manager", result);
    }

    [Fact]
    public void Humanize_DiskFull_TranslatesProperly()
    {
        var raw = "There is not enough space on the disk.";
        var result = ErrorFormatter.Humanize(raw);

        Assert.Contains("hết dung lượng trống", result);
    }

    [Fact]
    public void Humanize_NetworkError_TranslatesProperly()
    {
        var ex = new HttpRequestException("No such host is known.");
        var result = ErrorFormatter.Humanize(null, ex);

        Assert.Contains("kết nối mạng", result);
    }

    [Fact]
    public void ResultFail_AutomaticallyHumanizesErrorMessage()
    {
        var raw = @"Access to the path 'C:\Program Files\Wuthering Waves' is denied.";
        var r = Result.Fail("Cài lỗi: " + raw);

        Assert.False(r.Success);
        Assert.StartsWith("Cài đặt thất bại:", r.Error);
        Assert.Contains("Run as administrator", r.Error);
    }
}
