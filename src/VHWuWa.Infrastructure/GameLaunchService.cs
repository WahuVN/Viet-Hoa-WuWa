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

    public string GetOfficialLauncherPath(string gamePath) => Path.Combine(
        gamePath ?? string.Empty, "Wuthering Waves.exe");

    public Result Launch(string gamePath, bool forceCSharpEnvironment)
    {
        try
        {
            var clientExe = GetExecutablePath(gamePath);
            if (!File.Exists(clientExe))
                return Result.Fail("Không tìm thấy Client\\Binaries\\Win64\\Client-Win64-Shipping.exe. Hãy chọn lại đúng thư mục game.");

            var officialLauncher = GetOfficialLauncherPath(gamePath);
            var launchExe = File.Exists(officialLauncher) ? officialLauncher : clientExe;

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

            ProcessStartInfo startInfo;
            var helperExe = Environment.ProcessPath;
            var canUseSelfWatchdog = !string.IsNullOrWhiteSpace(helperExe)
                                     && File.Exists(helperExe)
                                     && string.Equals(Path.GetFileNameWithoutExtension(helperExe), "VHWuWa",
                                         StringComparison.OrdinalIgnoreCase);

            if (canUseSelfWatchdog)
            {
                // The watchdog launches the official wrapper when present, then
                // follows the newly spawned Client-Win64-Shipping child PID.
                startInfo = CreateWatchdogStartInfo(helperExe!, launchExe, forceCSharpEnvironment);
            }
            else
            {
                // Development/test fallback when hosted by dotnet/testhost.
                startInfo = CreateStartInfo(clientExe, forceCSharpEnvironment);
            }

            using var startedProcess = Process.Start(startInfo);
            if (startedProcess is null)
                return Result.Fail("Windows không thể khởi chạy game.");

            _log.Info("LaunchGame", canUseSelfWatchdog
                ? (forceCSharpEnvironment
                    ? "Đã mở launcher chuẩn qua Exit Watchdog với môi trường C# thử nghiệm."
                    : "Đã mở launcher chuẩn qua Exit Watchdog 5 giây.")
                : (forceCSharpEnvironment
                    ? "Đã mở game trực tiếp với môi trường C# thử nghiệm."
                    : "Đã mở game trực tiếp ở chế độ thường."));
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

    public static ProcessStartInfo CreateWatchdogStartInfo(
        string helperExecutablePath,
        string gameExecutablePath,
        bool forceCSharpEnvironment)
    {
        var info = new ProcessStartInfo
        {
            FileName = helperExecutablePath,
            WorkingDirectory = Path.GetDirectoryName(helperExecutablePath) ?? string.Empty,
            UseShellExecute = true,
            Verb = "runas"
        };
        info.ArgumentList.Add("--game-exit-watchdog");
        info.ArgumentList.Add("--game-exe");
        info.ArgumentList.Add(gameExecutablePath);
        info.ArgumentList.Add("--force-csharp");
        info.ArgumentList.Add(forceCSharpEnvironment ? "true" : "false");
        return info;
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
