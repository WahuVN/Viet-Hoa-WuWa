using System.ComponentModel;
using System.Diagnostics;
using VHWuWa.Core.Services;

namespace VHWuWa.Infrastructure;

public sealed record GameCloseResult(int Matched, int Closed, int Remaining, int Failures, int Ignored);

/// <summary>
/// Manual close, initiated by the user's confirmation. No global name-only Kill.
/// Missing process paths and unrelated installations fail closed.
/// </summary>
public static class GameProcessCloseService
{
    private static readonly string[] GameProcessNames = ["Client-Win64-Shipping", "Wuthering Waves"];

    public static GameCloseResult Inspect(string gamePath) => Operate(gamePath, ActionMode.Inspect);
    public static GameCloseResult RequestGracefulClose(string gamePath) => Operate(gamePath, ActionMode.Graceful);
    public static GameCloseResult ForceCloseConfirmed(string gamePath) => Operate(gamePath, ActionMode.Force);

    private enum ActionMode { Inspect, Graceful, Force }

    private static GameCloseResult Operate(string gamePath, ActionMode mode)
    {
        if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
            return new GameCloseResult(0, 0, 0, 0, 0);

        var matched = 0;
        var closed = 0;
        var remaining = 0;
        var failures = 0;
        var ignored = 0;

        foreach (var name in GameProcessNames)
        {
            foreach (var process in Process.GetProcessesByName(name))
            {
                using (process)
                {
                    try
                    {
                        if (process.HasExited) continue;

                        // Open the actual executable path, not just the process name.
                        var fullPath = process.MainModule?.FileName;
                        if (!GameProcessClosePolicy.IsSelectedGameProcess(gamePath, name, fullPath))
                        {
                            ignored++;
                            continue;
                        }

                        matched++;
                        if (mode == ActionMode.Inspect)
                        {
                            remaining++;
                            continue;
                        }

                        if (mode == ActionMode.Graceful)
                        {
                            // A windowed game can save data in its WM_CLOSE handler.
                            // Without a main window, never silently force-kill.
                            if (process.MainWindowHandle == nint.Zero || !process.CloseMainWindow())
                            {
                                remaining++;
                                continue;
                            }
                        }
                        else
                        {
                            // Revalidate the path at the last moment before a forced stop.
                            if (!GameProcessClosePolicy.IsSelectedGameProcess(
                                    gamePath, name, process.MainModule?.FileName))
                            {
                                ignored++;
                                continue;
                            }
                            process.Kill(entireProcessTree: false);
                        }

                        if (process.WaitForExit(mode == ActionMode.Graceful ? 5_000 : 3_000))
                            closed++;
                        else
                            remaining++;
                    }
                    catch (Exception ex) when (ex is Win32Exception or InvalidOperationException
                                                or UnauthorizedAccessException or SystemException)
                    {
                        failures++;
                    }
                }
            }
        }

        return new GameCloseResult(matched, closed, remaining, failures, ignored);
    }
}