using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
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

    // Steam requires its own client protocol; direct Client-Win64-Shipping can
    // be rejected with "Use launcher to start game!". Read app ID only from
    // the matching, installed appmanifest; never guess a Steam app ID.
    public static string? GetSteamLaunchUri(string gamePath)
    {
        if (string.IsNullOrWhiteSpace(gamePath)) return null;
        try
        {
            var normalized = Path.GetFullPath(gamePath.Trim().Trim('"'));
            var current = new DirectoryInfo(normalized);
            for (var depth = 0; current is not null && depth < 8; depth++, current = current.Parent)
            {
                if (!current.Name.Equals("common", StringComparison.OrdinalIgnoreCase)
                    || current.Parent is null
                    || !current.Parent.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
                    continue;

                var relative = Path.GetRelativePath(current.FullName, normalized);
                var installFolder = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
                if (installFolder.Length == 0 || installFolder == "..") return null;

                foreach (var manifest in Directory.EnumerateFiles(current.Parent.FullName,
                             "appmanifest_*.acf", SearchOption.TopDirectoryOnly))
                {
                    var fileName = Path.GetFileNameWithoutExtension(manifest);
                    if (!fileName.StartsWith("appmanifest_", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var appId = fileName["appmanifest_".Length..];
                    if (!Regex.IsMatch(appId, "^[0-9]{1,10}$")
                        || new FileInfo(manifest).Length > 1024 * 1024) continue;

                    var match = Regex.Match(File.ReadAllText(manifest),
                        "\"installdir\"\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
                    if (match.Success
                        && match.Groups[1].Value.Equals(installFolder, StringComparison.OrdinalIgnoreCase))
                        return "steam://rungameid/" + appId;
                }
                return null;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or ArgumentException or NotSupportedException)
        {
            return null;
        }
        return null;
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

            // Route a verified Steam installation through Steam itself, not the
            // raw shipping executable, and do not demand admin rights for a URI.
            var steamUri = GetSteamLaunchUri(gamePath);
            if (steamUri is not null)
            {
                Process.Start(new ProcessStartInfo(steamUri) { UseShellExecute = true });
                _log.Info("LaunchGame", "Đã gửi yêu cầu mở game qua Steam.");
                return Result.Ok();
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
                startInfo = CreateWatchdogStartInfo(helperExe!, launchExe, effectiveForceCSharp, clientExe);
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
        bool forceCSharpEnvironment, string? expectedClientExecutable = null)
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
        if (!string.IsNullOrWhiteSpace(expectedClientExecutable))
        {
            info.ArgumentList.Add("--expected-client-exe");
            info.ArgumentList.Add(expectedClientExecutable);
        }
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
