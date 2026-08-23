using System.ComponentModel;
using System.Diagnostics;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.Infrastructure;

public sealed class GameLaunchService : IGameLaunchService
{
    private readonly ILogService _log;

    public GameLaunchService(ILogService log) => _log = log;

    public string GetExecutablePath(string gamePath) => Path.Combine(
        gamePath ?? string.Empty, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");

    public Result Launch(string gamePath, bool forceCSharpEnvironment)
    {
        try
        {
            var exe = GetExecutablePath(gamePath);
            if (!File.Exists(exe))
                return Result.Fail("Không tìm thấy Client\\Binaries\\Win64\\Client-Win64-Shipping.exe. Hãy chọn lại đúng thư mục game.");

            var running = Process.GetProcessesByName("Client-Win64-Shipping");
            try
            {
                if (running.Length > 0)
                    return Result.Fail("Game đang chạy. Hãy đóng game trước khi mở lại bằng chế độ khác.");
            }
            finally
            {
                foreach (var process in running) process.Dispose();
            }

            var startInfo = CreateStartInfo(exe, forceCSharpEnvironment);
            using var startedProcess = Process.Start(startInfo);
            if (startedProcess is null)
                return Result.Fail("Windows không thể khởi chạy game.");

            _log.Info("LaunchGame", forceCSharpEnvironment
                ? "Đã mở game trực tiếp với môi trường C# thử nghiệm."
                : "Đã mở game trực tiếp ở chế độ thường.");
            return Result.Ok();
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            _log.Warn("LaunchGame", "Người dùng đã hủy hộp xác nhận quyền quản trị.");
            return Result.Fail("Bạn đã hủy yêu cầu quyền quản trị nên game chưa được mở.");
        }
        catch (Exception ex)
        {
            _log.Error("LaunchGame", "Không thể mở game: " + ex.Message, ex);
            return Result.Fail("Không thể mở game: " + ex.Message, ex);
        }
    }

    public static ProcessStartInfo CreateStartInfo(string executablePath, bool forceCSharpEnvironment)
    {
        var info = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty,
            UseShellExecute = true,
            Verb = "runas"
        };
        if (forceCSharpEnvironment)
            info.ArgumentList.Add(GameLaunchOptions.ForceCSharpEnvironment);
        return info;
    }
}
