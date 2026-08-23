using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using VHWuWa.Core.Services;

namespace VHWuWa.Updater;

/// <summary>Updater riêng: đóng app chính → backup → giải nén bản mới đè lên → relaunch.
/// Rollback nếu thất bại. KHÔNG ghi đè file đang chạy (app chính đã thoát).
/// Cách gọi: VHWuWa.Updater --zip &lt;file.zip&gt; --target &lt;appDir&gt; --relaunch VHWuWa.exe [--pid 1234]</summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        Console.InputEncoding = Encoding.UTF8;

        var o = Parse(args);
        if (!o.TryGetValue("zip", out var zip) || !o.TryGetValue("target", out var target))
        {
            Console.Error.WriteLine("Cách dùng: --zip <file.zip> --target <appDir> [--relaunch VHWuWa.exe] [--pid N]");
            return 1;
        }
        var relaunch = o.GetValueOrDefault("relaunch", "VHWuWa.exe");
        var noRelaunch = o.TryGetValue("no-relaunch", out var noRelaunchValue)
            && bool.TryParse(noRelaunchValue, out var parsedNoRelaunch) && parsedNoRelaunch;
        target = Path.GetFullPath(target);

        try
        {
            if (o.TryGetValue("pid", out var pidStr) && int.TryParse(pidStr, out var pid))
                WaitForExit(pid, TimeSpan.FromSeconds(30));

            if (!File.Exists(zip)) { Console.Error.WriteLine("Không tìm thấy file zip."); return 2; }
            Directory.CreateDirectory(target);
            var currentExe = Path.Combine(target, relaunch);
            if (!File.Exists(currentExe))
            {
                Console.Error.WriteLine("Thư mục đích không phải thư mục ứng dụng VHWuWa: " + target);
                return 2;
            }

            // Kiểm tra layout trước khi tạo backup. Lỗi ZIP sai cấu trúc không được
            // phép đụng tới bản đang dùng.
            using (var archive = ZipFile.OpenRead(zip))
                _ = UpdateArchiveInstaller.DetectApplicationPrefix(archive);

            var backupDir = target.TrimEnd('\\', '/') + "_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Console.WriteLine("Sao lưu bản hiện tại...");
            CopyDir(target, backupDir);

            try
            {
                Console.WriteLine("Giải nén bản cập nhật...");
                UpdateArchiveInstaller.ExtractApplication(zip, target);
                var exe = Path.Combine(target, relaunch);
                if (!File.Exists(exe)) throw new FileNotFoundException("Thiếu file thực thi sau cập nhật: " + relaunch);

                Console.WriteLine("Cập nhật thành công. Khởi động lại...");
                if (!noRelaunch)
                    Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = target });
                TryDelete(backupDir);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Cập nhật lỗi, đang rollback: " + ex.Message);
                RestoreDir(backupDir, target);
                var exe = Path.Combine(target, relaunch);
                if (!noRelaunch && File.Exists(exe))
                    Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = target });
                return 3;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Lỗi updater: " + ex.Message);
            return 4;
        }
    }

    private static void WaitForExit(int pid, TimeSpan timeout)
    {
        try
        {
            var p = Process.GetProcessById(pid);
            p.WaitForExit((int)timeout.TotalMilliseconds);
        }
        catch { /* tiến trình đã thoát */ }
    }

    private static void CopyDir(string src, string dst)
    {
        if (!Directory.Exists(src)) return;
        foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(src, dst));
        foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            var t = file.Replace(src, dst);
            Directory.CreateDirectory(Path.GetDirectoryName(t)!);
            File.Copy(file, t, overwrite: true);
        }
    }

    private static void RestoreDir(string backup, string target)
    {
        if (!Directory.Exists(backup)) return;
        if (Directory.Exists(target)) Directory.Delete(target, true);
        Directory.CreateDirectory(target);
        CopyDir(backup, target);
    }

    private static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); } catch { }
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--")) continue;
            var key = args[i][2..];
            var val = (i + 1 < args.Length && !args[i + 1].StartsWith("--")) ? args[++i] : "true";
            d[key] = val;
        }
        return d;
    }
}
