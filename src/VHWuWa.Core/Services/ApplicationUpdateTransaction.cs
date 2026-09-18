using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

namespace VHWuWa.Core.Services;

public enum UpdateTransactionCheckpoint
{
    CandidatePrepared,
    OriginalMoved,
    CandidateActivated,
}

/// <summary>
/// Cài một ZIP cập nhật bằng thư mục candidate cùng ổ đĩa và hoán đổi thư mục.
/// Bản cũ được giữ nguyên cho tới khi caller xác nhận app mới đã khởi động tốt.
/// </summary>
public sealed class ApplicationUpdateTransaction : IDisposable
{
    private readonly Action<string>? _log;
    private bool _finished;

    private ApplicationUpdateTransaction(string targetDirectory, string backupDirectory,
        Action<string>? log)
    {
        TargetDirectory = targetDirectory;
        BackupDirectory = backupDirectory;
        _log = log;
    }

    public string TargetDirectory { get; }
    public string BackupDirectory { get; }

    public static ApplicationUpdateTransaction Begin(
        string zipPath,
        string targetDirectory,
        string? expectedVersion,
        string? expectedSha256,
        Action<string>? log = null,
        Action<UpdateTransactionCheckpoint>? checkpoint = null)
    {
        var target = NormalizeTarget(targetDirectory);
        var zip = Path.GetFullPath(zipPath);
        var currentExe = Path.Combine(target, UpdateArchiveInstaller.MainExecutable);
        if (!File.Exists(currentExe))
            throw new InvalidDataException("Thư mục đích không chứa VHWuWa.exe: " + target);
        if (!File.Exists(zip))
            throw new FileNotFoundException("Không tìm thấy ZIP cập nhật.", zip);

        if (!string.IsNullOrWhiteSpace(expectedSha256))
            VerifySha256(zip, expectedSha256);

        long archiveLength;
        using (var archive = ZipFile.OpenRead(zip))
        {
            _ = UpdateArchiveInstaller.DetectApplicationPrefix(archive);
            archiveLength = archive.Entries.Sum(entry => entry.Length);
        }

        var parent = Path.GetDirectoryName(target)
            ?? throw new InvalidDataException("Không xác định được thư mục cha của ứng dụng.");
        var leaf = Path.GetFileName(target);
        var suffix = Guid.NewGuid().ToString("N");
        var candidate = Path.Combine(parent, $".{leaf}.update-{suffix}");
        var backup = Path.Combine(parent, $".{leaf}.backup-{suffix}");
        var failed = Path.Combine(parent, $".{leaf}.failed-{suffix}");
        var originalMoved = false;
        var candidateActivated = false;

        try
        {
            EnsureFreeSpace(target, archiveLength);
            log?.Invoke("Đang tạo bản candidate từ ứng dụng hiện tại...");
            CopyDirectory(target, candidate);
            UpdateArchiveInstaller.ExtractApplication(zip, candidate);
            ValidateCandidate(candidate, currentExe, expectedVersion);
            checkpoint?.Invoke(UpdateTransactionCheckpoint.CandidatePrepared);

            log?.Invoke("Đang kích hoạt phiên bản mới...");
            Directory.Move(target, backup);
            originalMoved = true;
            checkpoint?.Invoke(UpdateTransactionCheckpoint.OriginalMoved);

            Directory.Move(candidate, target);
            candidateActivated = true;
            checkpoint?.Invoke(UpdateTransactionCheckpoint.CandidateActivated);

            return new ApplicationUpdateTransaction(target, backup, log);
        }
        catch (Exception updateError)
        {
            try
            {
                RestoreAfterFailedActivation(
                    target, backup, candidate, failed, originalMoved, candidateActivated, log);
            }
            catch (Exception rollbackError)
            {
                throw new AggregateException(
                    "Cập nhật lỗi và không thể tự khôi phục hoàn toàn. Bản mới lỗi được giữ lại để cứu dữ liệu.",
                    updateError,
                    rollbackError);
            }

            throw;
        }
    }

    public void Commit()
    {
        if (_finished) return;
        _finished = true;
        TryDeleteDirectory(BackupDirectory, _log);
    }

    public void Rollback()
    {
        if (_finished) return;

        var parent = Path.GetDirectoryName(TargetDirectory)!;
        var failed = Path.Combine(parent,
            $".{Path.GetFileName(TargetDirectory)}.failed-{Guid.NewGuid():N}");
        try
        {
            if (Directory.Exists(TargetDirectory))
                Directory.Move(TargetDirectory, failed);
            if (!Directory.Exists(BackupDirectory))
                throw new DirectoryNotFoundException("Không còn thư mục backup để rollback.");
            Directory.Move(BackupDirectory, TargetDirectory);
            _finished = true;
            TryDeleteDirectory(failed, _log);
        }
        catch
        {
            if (!Directory.Exists(TargetDirectory) && Directory.Exists(failed))
                Directory.Move(failed, TargetDirectory);
            throw;
        }
    }

    public void Dispose()
    {
        if (!_finished)
            Rollback();
    }

    private static string NormalizeTarget(string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
            throw new ArgumentException("Thư mục đích trống.", nameof(targetDirectory));

        var target = Path.GetFullPath(targetDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var root = Path.GetPathRoot(target)?.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(target) || string.Equals(target, root,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Không được cập nhật trực tiếp vào thư mục gốc ổ đĩa.");
        return target;
    }

    private static void VerifySha256(string path, string expectedSha256)
    {
        var expected = expectedSha256.Trim();
        if (expected.Length != 64 || !expected.All(Uri.IsHexDigit))
            throw new InvalidDataException("SHA-256 kỳ vọng không hợp lệ.");

        using var stream = File.OpenRead(path);
        var actual = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("SHA-256 ZIP cập nhật không khớp.");
    }

    private static void ValidateCandidate(string candidate, string currentExe,
        string? expectedVersion)
    {
        var candidateExe = Path.Combine(candidate, UpdateArchiveInstaller.MainExecutable);
        var candidateUpdater = Path.Combine(candidate, UpdateArchiveInstaller.UpdaterExecutable);
        if (!File.Exists(candidateExe))
            throw new InvalidDataException("Candidate thiếu VHWuWa.exe.");
        if (!File.Exists(candidateUpdater))
            throw new InvalidDataException("Candidate thiếu VHWuWa.Updater.exe cho lần cập nhật sau.");

        if (string.IsNullOrWhiteSpace(expectedVersion)) return;
        if (!Version.TryParse(expectedVersion, out var expected))
            throw new InvalidDataException("Phiên bản cập nhật kỳ vọng không hợp lệ: " + expectedVersion);

        var current = ReadThreePartVersion(currentExe, "ứng dụng hiện tại");
        var candidateMain = ReadThreePartVersion(candidateExe, "VHWuWa.exe mới");
        var candidateUpdaterVersion = ReadThreePartVersion(candidateUpdater, "updater mới");
        var expectedThree = ThreePart(expected);

        if (candidateMain != expectedThree || candidateUpdaterVersion != expectedThree)
            throw new InvalidDataException(
                $"Sai phiên bản trong ZIP: cần {expectedThree}, app={candidateMain}, updater={candidateUpdaterVersion}.");
        if (candidateMain <= current)
            throw new InvalidDataException(
                $"Từ chối cập nhật không tăng phiên bản: hiện tại {current}, gói {candidateMain}.");
    }

    private static Version ReadThreePartVersion(string executable, string label)
    {
        var raw = FileVersionInfo.GetVersionInfo(executable).FileVersion;
        if (!Version.TryParse(raw, out var version))
            throw new InvalidDataException($"Không đọc được phiên bản {label}: {raw ?? "(trống)"}.");
        return ThreePart(version);
    }

    private static Version ThreePart(Version version) =>
        new(version.Major, Math.Max(0, version.Minor), Math.Max(0, version.Build));

    private static void EnsureFreeSpace(string target, long archiveLength)
    {
        var root = Path.GetPathRoot(target);
        if (string.IsNullOrWhiteSpace(root)) return;

        var targetLength = GetDirectoryLength(target);
        var required = checked(targetLength + archiveLength + 128L * 1024 * 1024);
        var drive = new DriveInfo(root);
        if (drive.AvailableFreeSpace < required)
            throw new IOException(
                $"Không đủ dung lượng trống để cập nhật. Cần khoảng {required / 1024 / 1024:N0} MB.");
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        CopyDirectoryContents(source, destination);
    }

    private static void CopyDirectoryContents(string source, string destination)
    {
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.TopDirectoryOnly))
        {
            if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Không hỗ trợ junction/symlink trong thư mục ứng dụng: " + directory);
            var childDestination = Path.Combine(destination, Path.GetFileName(directory));
            Directory.CreateDirectory(childDestination);
            CopyDirectoryContents(directory, childDestination);
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.TopDirectoryOnly))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Không hỗ trợ symlink trong thư mục ứng dụng: " + file);
            var target = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, target, overwrite: true);
        }
    }

    private static long GetDirectoryLength(string directory)
    {
        long length = 0;
        foreach (var child in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
        {
            if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Không hỗ trợ junction/symlink trong thư mục ứng dụng: " + child);
            length = checked(length + GetDirectoryLength(child));
        }
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Không hỗ trợ symlink trong thư mục ứng dụng: " + file);
            length = checked(length + new FileInfo(file).Length);
        }
        return length;
    }

    private static void RestoreAfterFailedActivation(string target, string backup, string candidate, string failed,
        bool originalMoved, bool candidateActivated, Action<string>? log)
    {
        try
        {
            if (candidateActivated && Directory.Exists(target))
                Directory.Move(target, failed);
            if (originalMoved && Directory.Exists(backup) && !Directory.Exists(target))
                Directory.Move(backup, target);
            if (originalMoved && !Directory.Exists(target))
                throw new DirectoryNotFoundException(
                    "Không thể đưa thư mục backup trở lại vị trí ứng dụng.");
        }
        finally
        {
            TryDeleteDirectory(candidate, log);
            if (!originalMoved || Directory.Exists(target))
                TryDeleteDirectory(failed, log);
        }
    }

    private static void TryDeleteDirectory(string path, Action<string>? log)
    {
        if (!Directory.Exists(path)) return;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            log?.Invoke("Không dọn được thư mục tạm/backup: " + path + " — " + ex.Message);
        }
    }
}
