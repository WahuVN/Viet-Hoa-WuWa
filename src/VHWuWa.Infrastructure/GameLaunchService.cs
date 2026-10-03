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

    public string GetOfficialLauncherPath(string gamePath)
    {
        var root = (gamePath ?? string.Empty).TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(root))
            return string.Empty;

        // Since WuWa 3.7 the in-game `Wuthering Waves.exe` wrapper can reject
        // direct launches with "Use launcher to start game!". The real Kuro
        // launcher normally lives one directory above `Wuthering Waves Game`.
        var parent = Directory.GetParent(root)?.FullName;
        if (!string.IsNullOrWhiteSpace(parent))
        {
            var kuroLauncher = Path.Combine(parent, "launcher.exe");
            if (File.Exists(kuroLauncher))
                return kuroLauncher;
        }

        // Some custom layouts may keep launcher.exe beside Client.
        var localLauncher = Path.Combine(root, "launcher.exe");
        if (File.Exists(localLauncher))
            return localLauncher;

        // Legacy fallback for older layouts only.
        return Path.Combine(root, "Wuthering Waves.exe");
    }

    public Result Launch(string gamePath, bool forceCSharpEnvironment)
    {
        try
        {
            var clientExe = GetExecutablePath(gamePath);
            if (!File.Exists(clientExe))
                return Result.Fail("Không tìm thấy Client\\Binaries\\Win64\\Client-Win64-Shipping.exe. Hãy chọn lại đúng thư mục game.");

            var officialLauncher = GetOfficialLauncherPath(gamePath);
            var launchExe = File.Exists(officialLauncher) ? officialLauncher : clientExe;
            var launchIsClient = string.Equals(
                Path.GetFileNameWithoutExtension(launchExe),
                "Client-Win64-Shipping",
                StringComparison.OrdinalIgnoreCase);
            // -ForceEnableCSharpEnvironment is a client argument, not a Kuro launcher argument.
            // Never pass it to launcher.exe / Wuthering Waves.exe wrappers.
            var effectiveForceCSharp = forceCSharpEnvironment && launchIsClient;

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
                startInfo = CreateWatchdogStartInfo(helperExe!, launchExe, effectiveForceCSharp);
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
                ? (launchIsClient && effectiveForceCSharp
                    ? "Đã mở client fallback qua Exit Watchdog với môi trường C# thử nghiệm."
                    : $"Đã mở launcher chuẩn qua Exit Watchdog: {Path.GetFileName(launchExe)}.")
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
