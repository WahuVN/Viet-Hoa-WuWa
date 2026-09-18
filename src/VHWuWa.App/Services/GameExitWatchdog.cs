using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using VHWuWa.Core.Models;

namespace VHWuWa.App.Services;

/// <summary>
/// Elevated helper used to launch the official game wrapper and clean up only a
/// confirmed no-window zombie. It never injects into, subclasses, or patches game memory.
/// </summary>
internal static class GameExitWatchdog
{
    private const string WatchdogFlag = "--game-exit-watchdog";
    private const int PollMs = 250;
    private const int ClientSpawnTimeoutMs = 120_000;
    private const int FirstWindowTimeoutMs = 180_000;
    private const int StableWindowMs = 10_000;
    private const int MissingWindowGraceMs = 5_000;
    private const int WrapperExitGraceMs = 5_000;

    private delegate bool EnumWindowsProc(nint hwnd, nint lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, nint lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(nint hwnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);

    public static bool IsWatchdogInvocation(string[] args) =>
        args.Any(arg => string.Equals(arg, WatchdogFlag, StringComparison.OrdinalIgnoreCase));

    public static int Run(string[] args)
    {
        try
        {
            var options = ParseArguments(args);
            if (!options.TryGetValue("game-exe", out var launchExe) ||
                string.IsNullOrWhiteSpace(launchExe) || !File.Exists(launchExe))
            {
                Log("invalid or missing --game-exe");
                return 2;
            }

            var forceCSharp = options.TryGetValue("force-csharp", out var forceText) &&
                              bool.TryParse(forceText, out var parsedForce) && parsedForce;

            var existingClientPids = CaptureClientPids();
            var startInfo = new ProcessStartInfo
            {
                FileName = launchExe,
                WorkingDirectory = Path.GetDirectoryName(launchExe) ?? string.Empty,
                UseShellExecute = true
            };
            if (forceCSharp)
                startInfo.ArgumentList.Add(GameLaunchOptions.ForceCSharpEnvironment);

            using var launcher = Process.Start(startInfo);
            if (launcher is null)
            {
                Log("Process.Start returned null");
                return 3;
            }

            Log($"launcher started pid={launcher.Id} exe={Path.GetFileName(launchExe)} forceCSharp={forceCSharp}");

            Process? game = null;
            var launchIsClient = string.Equals(
                Path.GetFileNameWithoutExtension(launchExe),
                "Client-Win64-Shipping",
                StringComparison.OrdinalIgnoreCase);

            if (launchIsClient)
            {
                game = launcher;
            }
            else
            {
                game = WaitForNewClient(existingClientPids, ClientSpawnTimeoutMs);
                if (game is null)
                {
                    Log("Client-Win64-Shipping did not appear within 120s; watchdog exits fail-open");
                    return 0;
                }
            }

            try
            {
                Log($"tracking client pid={game.Id} launcherPid={launcher.Id}");
                return Watch(game, launcher);
            }
            finally
            {
                if (game.Id != launcher.Id)
                    game.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log("fatal: " + ex);
            return 4;
        }
    }

    private static int Watch(Process game, Process launcher)
    {
        var firstWindowDeadline = Environment.TickCount64 + FirstWindowTimeoutMs;
        nint window = nint.Zero;
        while (Environment.TickCount64 < firstWindowDeadline)
        {
            if (HasExited(game))
            {
                Log("client exited before UnrealWindow appeared");
                CleanupWrapperAfterClientExit(launcher, game.Id);
                return 0;
            }

            window = FindOwnedUnrealWindow(game.Id);
            if (window != nint.Zero) break;
            Thread.Sleep(PollMs);
        }

        if (window == nint.Zero)
        {
            Log("UnrealWindow not observed within 180s; watchdog exits fail-open");
            return 0;
        }

        // A startup window must exist continuously before the watchdog is armed.
        // This prevents a transient create/destroy during boot from being treated as exit.
        var stableSince = Environment.TickCount64;
        while (Environment.TickCount64 - stableSince < StableWindowMs)
        {
            if (HasExited(game))
            {
                Log("client exited during window stabilization");
                CleanupWrapperAfterClientExit(launcher, game.Id);
                return 0;
            }

            if (FindOwnedUnrealWindow(game.Id) == nint.Zero)
                stableSince = Environment.TickCount64;
            Thread.Sleep(PollMs);
        }

        Log($"armed clientPid={game.Id} launcherPid={launcher.Id}; missing-window grace={MissingWindowGraceMs}ms");
        long missingSince = 0;

        for (;;)
        {
            if (HasExited(game))
            {
                Log("client exited normally");
                CleanupWrapperAfterClientExit(launcher, game.Id);
                return 0;
            }

            window = FindOwnedUnrealWindow(game.Id);
            if (window != nint.Zero)
            {
                missingSince = 0;
            }
            else
            {
                if (missingSince == 0)
                {
                    missingSince = Environment.TickCount64;
                    Log("UnrealWindow missing; starting 5s zombie grace");
                }
                else if (Environment.TickCount64 - missingSince >= MissingWindowGraceMs)
                {
                    Thread.Sleep(PollMs);
                    if (FindOwnedUnrealWindow(game.Id) != nint.Zero)
                    {
                        missingSince = 0;
                        Log("UnrealWindow returned on final check; cleanup cancelled");
                        continue;
                    }

                    if (!HasExited(game))
                    {
                        Log("confirmed no-window zombie; terminating tracked client process tree");
                        SafeKill(game, "client");
                    }
                    CleanupWrapperAfterClientExit(launcher, game.Id);
                    return 0;
                }
            }

            Thread.Sleep(PollMs);
        }
    }

    private static HashSet<int> CaptureClientPids()
    {
        var ids = new HashSet<int>();
        foreach (var process in Process.GetProcessesByName("Client-Win64-Shipping"))
        {
            try { ids.Add(process.Id); }
            finally { process.Dispose(); }
        }
        return ids;
    }

    private static Process? WaitForNewClient(HashSet<int> existingPids, int timeoutMs)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            foreach (var process in Process.GetProcessesByName("Client-Win64-Shipping"))
            {
                if (!existingPids.Contains(process.Id))
                    return process;
                process.Dispose();
            }
            Thread.Sleep(PollMs);
        }
        return null;
    }

    private static void CleanupWrapperAfterClientExit(Process launcher, int clientPid)
    {
        if (launcher.Id == clientPid || HasExited(launcher)) return;

        var deadline = Environment.TickCount64 + WrapperExitGraceMs;
        while (Environment.TickCount64 < deadline)
        {
            if (HasExited(launcher)) return;
            Thread.Sleep(PollMs);
        }

        if (!HasExited(launcher))
        {
            Log("official wrapper still alive 5s after client exit; terminating owned wrapper");
            SafeKill(launcher, "wrapper");
        }
    }

    private static void SafeKill(Process process, string label)
    {
        try
        {
            if (HasExited(process)) return;
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5_000);
            Log($"{label} cleanup complete");
        }
        catch (Exception ex)
        {
            Log($"{label} cleanup failed: {ex.Message}");
        }
    }

    private static bool HasExited(Process process)
    {
        try { return process.HasExited; }
        catch { return true; }
    }

    private static nint FindOwnedUnrealWindow(int targetPid)
    {
        nint found = nint.Zero;
        EnumWindows((hwnd, _) =>
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            if (pid != (uint)targetPid) return true;

            var className = new StringBuilder(128);
            if (GetClassNameW(hwnd, className, className.Capacity) <= 0) return true;
            if (!string.Equals(className.ToString(), "UnrealWindow", StringComparison.OrdinalIgnoreCase))
                return true;

            found = hwnd;
            return false;
        }, nint.Zero);
        return found;
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) continue;
            var key = args[i][2..];
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
            result[key] = value;
        }
        return result;
    }

    private static void Log(string message)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VHWuWa");
            Directory.CreateDirectory(dir);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(dir, "exit-watchdog.log"), line, Encoding.UTF8);
        }
        catch
        {
            // Logging must never affect launching or cleanup.
        }
    }
}
