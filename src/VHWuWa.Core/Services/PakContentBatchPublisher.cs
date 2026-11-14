using System.Security.Cryptography;
using System.Text.Json;

namespace VHWuWa.Core.Services;

/// <summary>
/// Publishes verified EN/HanViet content as a recoverable batch without touching other mods.
/// All payloads must be downloaded and SHA-256 verified before the first installed PAK moves.
/// </summary>
public static class PakContentBatchPublisher
{
    private const string Prefix = ".wuwa-pak-txn-";
    private const string JournalName = "journal.json";
    private const string CommitName = "committed";
    private static readonly HashSet<string> AllowedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "WuWaVH_EN_99_P.pak",
        "WuWaVH_HanViet_99_P.pak",
    };

    public sealed record Payload(string FileName, string SourcePath, string ExpectedSha256);
    private sealed record Entry(string Name, bool Existed);
    private sealed record Journal(List<Entry> Files);

    public static void Publish(string destinationDirectory, IReadOnlyCollection<Payload> payloads,
        Action<int>? afterActivation = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);
        ArgumentNullException.ThrowIfNull(payloads);
        if (payloads.Count is < 1 or > 2)
            throw new InvalidDataException("Cần 1 hoặc 2 PAK bản dịch hợp lệ.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in payloads)
        {
            ValidateName(file.FileName);
            if (!seen.Add(file.FileName))
                throw new InvalidDataException("Gói PAK cập nhật bị trùng tên.");
            Verify(file.SourcePath, file.ExpectedSha256);
        }

        var target = Path.GetFullPath(destinationDirectory);
        Directory.CreateDirectory(target);
        RecoverIncomplete(target);

        var transactionDir = Path.Combine(target, Prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(transactionDir);
        var journalWritten = false;
        try
        {
            var entries = new List<Entry>();
            foreach (var file in payloads)
            {
                var dest = Path.Combine(target, file.FileName);
                if (Directory.Exists(dest))
                    throw new IOException("Đích PAK bị chiếm bởi thư mục: " + file.FileName);
                var existing = File.Exists(dest);
                entries.Add(new Entry(file.FileName, existing));
                File.Copy(file.SourcePath, Path.Combine(transactionDir, file.FileName + ".new"));
                Verify(Path.Combine(transactionDir, file.FileName + ".new"), file.ExpectedSha256);
                if (existing)
                    File.Copy(dest, Path.Combine(transactionDir, file.FileName + ".bak"));
            }

            // The journal is durable before any installed file is replaced.
            WriteDurable(Path.Combine(transactionDir, JournalName),
                JsonSerializer.Serialize(new Journal(entries)));
            journalWritten = true;

            var activated = 0;
            foreach (var file in payloads)
            {
                File.Move(Path.Combine(transactionDir, file.FileName + ".new"),
                    Path.Combine(target, file.FileName), overwrite: true);
                afterActivation?.Invoke(++activated);
            }

            WriteDurable(Path.Combine(transactionDir, CommitName), "ok");
            TryDelete(transactionDir);
        }
        catch (Exception updateFailure)
        {
            if (journalWritten)
            {
                try
                {
                    RecoverTransaction(target, transactionDir);
                }
                catch (Exception rollbackFailure)
                {
                    throw new AggregateException(
                        "Cập nhật PAK lỗi và tự khôi phục thất bại; bản sao lưu còn tại " + transactionDir,
                        updateFailure, rollbackFailure);
                }
            }
            else
            {
                TryDelete(transactionDir);
            }
            throw;
        }
    }

    /// <summary>Call on startup and before any new update to restore interrupted batches.</summary>
    public static void RecoverIncomplete(string destinationDirectory)
    {
        if (!Directory.Exists(destinationDirectory)) return;
        foreach (var transactionDir in Directory.EnumerateDirectories(destinationDirectory, Prefix + "*",
                     SearchOption.TopDirectoryOnly))
        {
            if (!File.Exists(Path.Combine(transactionDir, JournalName)))
                continue; // No target file can be changed before the journal exists.
            RecoverTransaction(destinationDirectory, transactionDir);
        }
    }

    private static void RecoverTransaction(string target, string transactionDir)
    {
        var journalPath = Path.Combine(transactionDir, JournalName);
        var journal = JsonSerializer.Deserialize<Journal>(File.ReadAllText(journalPath))
            ?? throw new InvalidDataException("Nhật ký cập nhật PAK không hợp lệ.");
        if (journal.Files.Count is < 1 or > 2)
            throw new InvalidDataException("Nhật ký cập nhật PAK sai số lượng.");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in journal.Files)
        {
            ValidateName(entry.Name);
            if (!seen.Add(entry.Name))
                throw new InvalidDataException("Nhật ký cập nhật PAK trùng tên.");
            if (entry.Existed && !File.Exists(Path.Combine(transactionDir, entry.Name + ".bak"))
                && !File.Exists(Path.Combine(transactionDir, CommitName)))
                throw new IOException("Thiếu PAK sao lưu để khôi phục: " + entry.Name);
        }
        if (File.Exists(Path.Combine(transactionDir, CommitName)))
        {
            TryDelete(transactionDir);
            return;
        }
        foreach (var entry in journal.Files)
        {
            var dest = Path.Combine(target, entry.Name);
            if (entry.Existed)
            {
                var restore = Path.Combine(transactionDir, entry.Name + ".restore");
                File.Copy(Path.Combine(transactionDir, entry.Name + ".bak"), restore, overwrite: true);
                File.Move(restore, dest, overwrite: true);
            }
            else if (File.Exists(dest))
            {
                File.Delete(dest);
            }
        }
        TryDelete(transactionDir);
    }

    private static void ValidateName(string name)
    {
        if (!AllowedNames.Contains(name))
            throw new InvalidDataException("Không cho phép ghi PAK ngoài danh mục chính thức.");
    }

    private static void Verify(string file, string sha256)
    {
        if (string.IsNullOrWhiteSpace(sha256) || sha256.Length != 64 || !sha256.All(Uri.IsHexDigit))
            throw new InvalidDataException("PAK thiếu SHA-256 hợp lệ.");
        using var stream = File.OpenRead(file);
        var actual = Convert.ToHexString(SHA256.HashData(stream));
        if (!actual.Equals(sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("PAK không khớp SHA-256: " + Path.GetFileName(file));
    }

    private static void WriteDurable(string path, string text)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(file, leaveOpen: true);
        writer.Write(text);
        writer.Flush();
        file.Flush(flushToDisk: true);
    }

    private static void TryDelete(string directory)
    {
        try { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
        catch (IOException) { } // Committed content is safe; cleanup can be retried on startup.
    }
}
