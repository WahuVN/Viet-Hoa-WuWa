using System.IO;
using System.Runtime.InteropServices;

namespace VHWuWa.App.Services;

internal static class DesktopShortcutService
{
    private const string ShortcutFileName = "WuWa.lnk";
    private const string LegacyShortcutFileName = "VHWuWa.lnk";

    public static string EnsureCreated()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Shortcut Desktop chỉ được hỗ trợ trên Windows.");

        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
            throw new FileNotFoundException("Không xác định được VHWuWa.exe đang chạy.", executable);

        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
            throw new DirectoryNotFoundException("Không xác định được thư mục Desktop của người dùng.");

        var shortcutPath = Path.Combine(desktop, ShortcutFileName);
        var legacyShortcutPath = Path.Combine(desktop, LegacyShortcutFileName);
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("Windows Script Host không khả dụng.");
        object? shellObject = null;
        object? shortcutObject = null;

        try
        {
            if (File.Exists(legacyShortcutPath))
                File.Delete(legacyShortcutPath);

            shellObject = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Không khởi tạo được Windows Script Host.");
            dynamic shell = shellObject;
            shortcutObject = shell.CreateShortcut(shortcutPath);
            dynamic shortcut = shortcutObject;
            shortcut.TargetPath = executable;
            shortcut.WorkingDirectory = AppContext.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            shortcut.IconLocation = executable + ",0";
            // WScript.Shell có thể ghi mô tả Unicode sai theo code page của Windows.
            // Giữ tooltip ASCII để mọi máy đều hiển thị ổn định.
            shortcut.Description = "WuWa - Viet Hoa Wuthering Waves";
            shortcut.WindowStyle = 1;
            shortcut.Save();
            return shortcutPath;
        }
        finally
        {
            ReleaseComObject(shortcutObject);
            ReleaseComObject(shellObject);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
            Marshal.FinalReleaseComObject(value);
    }
}
