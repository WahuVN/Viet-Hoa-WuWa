using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using VHWuWa.Core.Services;

namespace VHWuWa.Updater;

/// <summary>
/// Updater ngoài tiến trình: xác minh gói, tạo candidate, hoán đổi thư mục,
/// chờ app mới báo khởi động thành công rồi mới xóa backup.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        Console.InputEncoding = Encoding.UTF8;

        var options = Parse(args);
        if (!TryGetRequired(options, "zip", out var zip)
            || !TryGetRequired(options, "target", out var target)
            || !TryGetRequired(options, "expected-version", out var expectedVersion)
            || !TryGetRequired(options, "sha256", out var expectedSha256))
        {
            Console.Error.WriteLine(
                "Cách dùng: --zip <file.zip> --target <appDir> --expected-version <x.y.z> "
                + "--sha256 <64 hex> [--relaunch VHWuWa.exe] [--pid N] "
                + "[--process-start-ticks N] [--cleanup-dir <dir>] [--no-relaunch]");
            return 1;
        }

        var relaunch = options.GetValueOrDefault("relaunch", UpdateArchiveInstaller.MainExecutable);
        var noRelaunch = options.ContainsKey("no-relaunch");
        var cleanupDirectory = options.GetValueOrDefault("cleanup-dir", string.Empty);
        ApplicationUpdateTransaction? transaction = null;
        Process? launchedApp = null;
        var parentHandoffComplete = false;

        try
        {
            zip = Path.GetFullPath(zip);
            target = Path.GetFullPath(target);
            // Refuse before touching ready-file, parent process or target directory.
            UpdateTargetGuard.EnsureSafeTarget(target);
            VerifySha256(zip, expectedSha256);
            ValidatePreflight(zip, target, relaunch, expectedVersion);
            // A production update must prove startup health. A ZIP-only offline swap is not a release-safe path.
            if (noRelaunch)
                throw new InvalidOperationException("Không cho phép cập nhật mà bỏ qua kiểm tra khởi động ứng dụng.");
            SignalReady(options, cleanupDirectory);
            parentHandoffComplete = options.ContainsKey("pid") && WaitForParent(options, TimeSpan.FromSeconds(30));

            transaction = ApplicationUpdateTransaction.Begin(
                zip,
                target,
                expectedVersion,
                expectedSha256,
                message => Console.WriteLine(message));

            var healthFile = Path.Combine(
                string.IsNullOrWhiteSpace(cleanupDirectory)
                    ? Path.GetDirectoryName(zip)!
                    : Path.GetFullPath(cleanupDirectory),
                "startup-health.txt");
            DeleteExistingHealthFile(healthFile);

            launchedApp = StartApplication(
                target,
                relaunch,
                expectedVersion,
                healthFile,
                cleanupDirectory,
                Environment.ProcessId);

            if (!WaitForHealthyStartup(launchedApp, healthFile, expectedVersion, TimeSpan.FromSeconds(30)))
                throw new InvalidOperationException(
                    "Ứng dụng mới không xác nhận khởi động thành công trong 30 giây.");

            transaction.Commit();
            transaction = null;
            Console.WriteLine("Cập nhật và kiểm tra khởi động thành công.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Cập nhật lỗi, đang khôi phục bản cũ: " + ex.Message);
            TryStop(launchedApp);

            if (transaction is not null)
            {
                try
                {
                    transaction.Rollback();
                    transaction = null;
                }
                catch (Exception rollbackError)
                {
                    Console.Error.WriteLine("ROLLBACK THẤT BẠI: " + rollbackError.Message);
                    // Giữ nguyên target/backup/failed để người dùng còn dữ liệu cứu hộ;
                    // không tự retry trong finally khi chưa biết trạng thái khóa file.
                    transaction = null;
                    return 5;
                }
            }

            if (!noRelaunch && parentHandoffComplete && Directory.Exists(target))
            {
                try
                {
                    _ = StartApplication(
                        target,
                        relaunch,
                        expectedVersion: null,
                        healthFile: null,
                        cleanupDirectory,
                        Environment.ProcessId);
                }
                catch (Exception restartError)
                {
                    Console.Error.WriteLine("Không thể mở lại bản cũ: " + restartError.Message);
                    return 6;
                }
            }

            return 4;
        }
        finally
        {
            launchedApp?.Dispose();
            if (transaction is not null)
            {
                try
                {
                    transaction.Dispose();
                }
                catch (Exception rollbackError)
                {
                    Console.Error.WriteLine("Không thể hoàn tất rollback: " + rollbackError.Message);
                }
            }
        }
    }

    private static Process StartApplication(
        string target,
        string relaunch,
        string? expectedVersion,
        string? healthFile,
        string cleanupDirectory,
        int updaterPid)
    {
        var executable = Path.GetFullPath(Path.Combine(target, relaunch));
        if (!PathValidation.IsSubPathOf(executable, target) || !File.Exists(executable))
            throw new FileNotFoundException("Không tìm thấy file thực thi hợp lệ để mở lại.", executable);

        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = target,
        };
        if (!string.IsNullOrWhiteSpace(healthFile) && !string.IsNullOrWhiteSpace(expectedVersion))
        {
            startInfo.ArgumentList.Add("--update-health-file");
            startInfo.ArgumentList.Add(healthFile);
            startInfo.ArgumentList.Add("--update-expected-version");
            startInfo.ArgumentList.Add(expectedVersion);
        }
        if (!string.IsNullOrWhiteSpace(cleanupDirectory))
        {
            startInfo.ArgumentList.Add("--update-cleanup-dir");
            startInfo.ArgumentList.Add(Path.GetFullPath(cleanupDirectory));
            startInfo.ArgumentList.Add("--update-updater-pid");
            startInfo.ArgumentList.Add(updaterPid.ToString());
            using var updater = Process.GetCurrentProcess();
            startInfo.ArgumentList.Add("--update-updater-start-ticks");
            startInfo.ArgumentList.Add(updater.StartTime.ToUniversalTime().Ticks.ToString());
        }

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Không khởi động được ứng dụng.");
    }

    private static bool WaitForHealthyStartup(
        Process app,
        string healthFile,
        string expectedVersion,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (app.HasExited) return false;
            if (File.Exists(healthFile))
            {
                try
                {
                    var reported = File.ReadAllText(healthFile).Trim();
                    if (!string.Equals(reported, NormalizeVersion(expectedVersion),
                            StringComparison.OrdinalIgnoreCase))
                        return false;
                    Thread.Sleep(1_500);
                    return !app.HasExited;
                }
                catch (IOException)
                {
                    // App có thể đang ghi file; thử lại ở vòng tiếp theo.
                }
            }

            Thread.Sleep(200);
        }

        return false;
    }

    private static bool WaitForParent(
        IReadOnlyDictionary<string, string> options,
        TimeSpan timeout)
    {
        if (!options.TryGetValue("pid", out var pidText) || !int.TryParse(pidText, out var pid))
            return true;

        try
        {
            using var parent = Process.GetProcessById(pid);
            if (options.TryGetValue("process-start-ticks", out var ticksText)
                && long.TryParse(ticksText, out var expectedTicks)
                && parent.StartTime.ToUniversalTime().Ticks != expectedTicks)
            {
                Console.WriteLine("PID đã được tái sử dụng; không chờ/đóng tiến trình không liên quan.");
                return true;
            }

            if (parent.WaitForExit((int)timeout.TotalMilliseconds)) return true;
            Console.WriteLine("Ứng dụng cũ chưa thoát sau 30 giây; đang đóng cưỡng bức đúng tiến trình.");
            parent.Kill(entireProcessTree: true);
            if (!parent.WaitForExit(10_000))
                throw new TimeoutException("Không thể đóng ứng dụng cũ.");
            return true;
        }
        catch (ArgumentException)
        {
            // Tiến trình đã thoát.
            return true;
        }
        catch (InvalidOperationException)
        {
            // Tiến trình đã thoát giữa lúc kiểm tra.
            return true;
        }
    }

    private static void ValidatePreflight(
        string zip,
        string target,
        string relaunch,
        string expectedVersion)
    {
        if (!Version.TryParse(expectedVersion, out _))
            throw new InvalidDataException("Phiên bản cập nhật kỳ vọng không hợp lệ.");
        var currentExecutable = Path.GetFullPath(Path.Combine(target, relaunch));
        if (!PathValidation.IsSubPathOf(currentExecutable, target) || !File.Exists(currentExecutable))
            throw new InvalidDataException("Thư mục đích không chứa ứng dụng hiện tại hợp lệ.");
        using var archive = ZipFile.OpenRead(zip);
        var prefix = UpdateArchiveInstaller.DetectApplicationPrefix(archive);
        // Verify both release EXE versions before writing ready-file or exiting the old app.
        var expected = Version.Parse(expectedVersion);
        var expectedThree = new Version(expected.Major, Math.Max(0, expected.Minor),
            Math.Max(0, expected.Build));
        var current = ReadVersion(currentExecutable);
        if (expectedThree <= current)
            throw new InvalidDataException("Gói cập nhật không tăng phiên bản.");

        var probe = Path.Combine(Path.GetTempPath(),
            "VHWuWa_Preflight_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(probe);
            foreach (var exe in new[] {
                         UpdateArchiveInstaller.MainExecutable,
                         UpdateArchiveInstaller.UpdaterExecutable })
            {
                var entry = archive.Entries.SingleOrDefault(e =>
                    e.FullName.Replace('\\', '/').Equals(prefix + exe,
                        StringComparison.OrdinalIgnoreCase));
                if (entry is null)
                    throw new InvalidDataException("ZIP thiếu executable bắt buộc: " + exe);
                var probeExe = Path.Combine(probe, exe);
                entry.ExtractToFile(probeExe);
                if (ReadVersion(probeExe) != expectedThree)
                    throw new InvalidDataException("Phiên bản ZIP không khớp: " + exe);
            }
        }
        finally
        {
            try { if (Directory.Exists(probe)) Directory.Delete(probe, true); }
            catch (IOException) { /* Report cleanup separately during implementation. */ }
        }
    }

    private static Version ReadVersion(string file)
    {
        var raw = FileVersionInfo.GetVersionInfo(file).FileVersion;
        if (!Version.TryParse(raw, out var v))
            throw new InvalidDataException("Không đọc được FileVersion: " + file);
        return new Version(v.Major, Math.Max(0, v.Minor), Math.Max(0, v.Build));
    }

    private static void SignalReady(
        IReadOnlyDictionary<string, string> options,
        string cleanupDirectory)
    {
        if (!options.TryGetValue("ready-file", out var readyFile)) return;
        var ready = Path.GetFullPath(readyFile);
        if (string.IsNullOrWhiteSpace(cleanupDirectory)
            || !PathValidation.IsSubPathOf(ready, Path.GetFullPath(cleanupDirectory)))
            throw new InvalidDataException("Đường dẫn ready-file không hợp lệ.");
        File.WriteAllText(ready, "ready");
    }

    private static void VerifySha256(string path, string expectedSha256)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Không tìm thấy ZIP cập nhật.", path);

        var expected = expectedSha256.Trim();
        if (expected.Length != 64 || !expected.All(Uri.IsHexDigit))
            throw new InvalidDataException("SHA-256 kỳ vọng không hợp lệ.");

        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(SHA256.HashData(stream));
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("SHA-256 ZIP cập nhật không khớp.");
    }

    private static string NormalizeVersion(string version)
    {
        if (!Version.TryParse(version, out var parsed)) return version.Trim();
        return new Version(parsed.Major, Math.Max(0, parsed.Minor), Math.Max(0, parsed.Build)).ToString();
    }

    private static void TryStop(Process? process)
    {
        if (process is null) return;
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5_000);
            }
        }
        catch
        {
            // Rollback vẫn được thử ngay cả khi không đóng được app mới.
        }
    }

    private static void DeleteExistingHealthFile(string path)
    {
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path))
            throw new IOException("Không thể xóa health file cũ trước khi kiểm tra app mới.");
    }

    private static bool TryGetRequired(
        IReadOnlyDictionary<string, string> options,
        string key,
        out string value)
    {
        if (options.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
        {
            value = raw;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static Dictionary<string, string> Parse(string[] args)
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
}
