using System.IO;

namespace VHWuWa.Core.Services;

/// <summary>
/// Resolve only actual game executables from a specifically selected game root.
/// The launcher and Steam are deliberately never valid manual-close targets.
/// </summary>
public static class GameProcessClosePolicy
{
    public static string? ExpectedExecutable(string gamePath, string processName)
    {
        if (string.IsNullOrWhiteSpace(gamePath)) return null;
        var relative = processName switch
        {
            "Client-Win64-Shipping" => Path.Combine("Client", "Binaries", "Win64", "Client-Win64-Shipping.exe"),
            "Wuthering Waves" => "Wuthering Waves.exe",
            _ => null
        };

        if (relative is null) return null;
        try
        {
            return Path.GetFullPath(Path.Combine(gamePath.Trim().Trim('"'), relative));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    public static bool IsSelectedGameProcess(string gamePath, string processName, string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath)) return false;
        var expected = ExpectedExecutable(gamePath, processName);
        if (expected is null) return false;
        try
        {
            return string.Equals(Path.GetFullPath(executablePath), expected, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }
}