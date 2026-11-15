using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using VHWuWa.Core.Models;

namespace VHWuWa.Infrastructure;

/// <summary>
/// PC R29 loader-free game-resource mount. The archive was repacked from R28 with the
/// same KuroV12 plaintext-index/mount-point envelope as the working phone and PC 3.7
/// versions. Runtime QA on the pinned PC build showed localized login UI.
/// This installer NEVER modifies official MountLang_*.txt, stock PAKs or Win64 DLLs.
/// </summary>
public static class R29MountedPak
{
    public const string Release = "R29";
    public const string ResourceVersion = "3.8.11";
    public const string ArchiveName = "WuWaVH_PC_R29_99_P.pak";
    public const string ArchiveSha256 = "38527E34FFEB8EF7538206D3D4DBD5CBAC54A1E6CD9D0957501035FF41A8E598";
    public const long ArchiveBytes = 125532999;

    private const string StagedPakName = "WuWaVH_99_P.pak";
    private const string StagedSigName = "WuWaVH_99_P.sig";
    private const string SidecarName = "wahuviethoa.txt";

    public static string Source(string appDirectory) =>
        Path.Combine(appDirectory, "content", ResourceVersion, ArchiveName);

    private static string GameResourceDir(string gameRoot) =>
        Path.Combine(gameRoot, "Client", "Saved", "Resources", "3.8.0");

    private static string MountDir(string gameRoot) =>
        Path.Combine(GameResourceDir(gameRoot), "Mount");

    private static string DestinationDir(string gameRoot) =>
        Path.Combine(GameResourceDir(gameRoot), "wahuviethoa");

    private static string DestinationPak(string gameRoot) =>
        Path.Combine(DestinationDir(gameRoot), StagedPakName);

    private static string DestinationSig(string gameRoot) =>
        Path.Combine(DestinationDir(gameRoot), StagedSigName);

    private static string DestinationMount(string gameRoot) =>
        Path.Combine(MountDir(gameRoot), SidecarName);

    /// <summary>
    /// Finds the English-language SIG in either a numbered patch directory or
    /// Lang_en/Base (used by full installs without incremental language patches).
    /// </summary>
    private static string? StockSig(string gameRoot)
    {
        var langEn = Path.Combine(GameResourceDir(gameRoot), "Lang_en");
        if (!Directory.Exists(langEn)) return null;
        string? latest = null;
        string? baseSig = null;
        Version? latestVersion = null;
        foreach (var dir in Directory.EnumerateDirectories(langEn, "*", SearchOption.TopDirectoryOnly))
        {
            var candidate = Path.Combine(dir, "pakchunk10-WindowsNoEditor_P.sig");
            if (!File.Exists(candidate) || new FileInfo(candidate).Length == 0) continue;
            var name = Path.GetFileName(dir);
            if (name.Equals("Base", StringComparison.OrdinalIgnoreCase))
            {
                baseSig = candidate;
                continue;
            }
            if (!Version.TryParse(name, out var version)) continue;
            if (latestVersion is null || version.CompareTo(latestVersion) > 0)
            {
                latest = candidate;
                latestVersion = version;
            }
        }
        if (latest is not null) return latest;
        var rootSig = Path.Combine(langEn, "pakchunk10-WindowsNoEditor_P.sig");
        return baseSig ?? (File.Exists(rootSig) && new FileInfo(rootSig).Length > 0 ? rootSig : null);
    }

    private static string OfficialMount(string gameRoot) =>
        Path.Combine(MountDir(gameRoot), "MountLang_en.txt");

    private static bool HasReparsePoint(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch { return true; }
    }

    private static string Sha(string path, HashAlgorithmName algorithm)
    {
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexString(algorithm == HashAlgorithmName.SHA1
            ? SHA1.HashData(input)
            : SHA256.HashData(input));
    }

    private static bool ShaMatches(string path, string expected) =>
        File.Exists(path) && Sha(path, HashAlgorithmName.SHA256)
            .Equals(expected, StringComparison.OrdinalIgnoreCase);

    private static string SidecarText(string pakSha1, string sigSha1) =>
        "::Mount::\r\nwahuviethoa/WuWaVH_99_P,99,"
        + pakSha1 + "," + sigSha1 + ",,\r\n::Del::\r\n";

    public static bool PackagePresent(string appDirectory) => File.Exists(Source(appDirectory));

    /// <summary>
    /// A 3.0.19 R29 release must never fall back to stale 3.7 loader/PAK files
    /// left behind by the 3.0.18 in-place updater if R29 content is missing.
    /// The release marker is also shipped with the final ZIP.
    /// </summary>
    public static bool ReleaseRequired(string appDirectory)
    {
        if (File.Exists(Path.Combine(appDirectory, "content", "r29-release.json")))
            return true;
        var entry = System.Reflection.Assembly.GetEntryAssembly()?.GetName();
        return string.Equals(entry?.Name, "VHWuWa", StringComparison.OrdinalIgnoreCase)
            && entry?.Version is { Major: 3, Minor: 0, Build: 19 };
    }

    public static bool PackageVerified(string appDirectory)
    {
        try
        {
            var file = Source(appDirectory);
            return File.Exists(file) && new FileInfo(file).Length == ArchiveBytes
                && ShaMatches(file, ArchiveSha256);
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    /// <summary>
    /// Checks only paths required by the independent R29 sidecar installer.
    /// Official MountLang_en.txt is never modified and is optional in a clean
    /// install if the Mount directory itself is present.
    /// </summary>
    public static string? CompatibilityIssue(string gameRoot)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gameRoot)) return "Chưa chọn thư mục game.";
            var exe = Path.Combine(gameRoot, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
            if (!File.Exists(exe)) return "Không tìm thấy Client/Binaries/Win64/Client-Win64-Shipping.exe.";
            if (!Directory.Exists(GameResourceDir(gameRoot)))
                return "Không tìm thấy Client/Saved/Resources/3.8.0. R29 cần dữ liệu game 3.8.0.";
            if (!Directory.Exists(MountDir(gameRoot)))
                return "Thiếu thư mục Resources/3.8.0/Mount của game.";
            if (StockSig(gameRoot) is null)
                return "Thiếu SIG tiếng Anh tại Lang_en/Base hoặc Lang_en/<phiên bản>. Hãy tải dữ liệu ngôn ngữ English trong game.";
            return null;
        }
        catch (IOException) { return "Không đọc được dữ liệu Mount/SIG của game."; }
        catch (UnauthorizedAccessException) { return "Không có quyền đọc Mount/SIG của game."; }
        catch (ArgumentException) { return "Đường dẫn game không hợp lệ."; }
    }

    public static bool GameCompatible(string gameRoot) => CompatibilityIssue(gameRoot) is null;

    public static bool IsInstalled(string gameRoot)
    {
        try
        {
            var pak = DestinationPak(gameRoot);
            var sig = DestinationSig(gameRoot);
            var sidecar = DestinationMount(gameRoot);
            if (!File.Exists(pak) || !File.Exists(sig) || !File.Exists(sidecar)
                || new FileInfo(pak).Length != ArchiveBytes
                || !ShaMatches(pak, ArchiveSha256)) return false;
            var expected = SidecarText(Sha(pak, HashAlgorithmName.SHA1),
                Sha(sig, HashAlgorithmName.SHA1));
            return string.Equals(File.ReadAllText(sidecar, Encoding.UTF8), expected,
                StringComparison.Ordinal);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Files from an incomplete or foreign mount using our exact R29 names may
    /// be backed up automatically before installing. Never touch the official
    /// game mount or SIG, and never remove an already healthy R29 install.
    /// </summary>
    public static IReadOnlyList<string> StaleActivationFiles(string gameRoot)
    {
        if (IsInstalled(gameRoot)) return Array.Empty<string>();
        return new[] { DestinationPak(gameRoot), DestinationSig(gameRoot), DestinationMount(gameRoot) }
            .Where(File.Exists).ToArray();
    }

    private static bool IsGameRunning() =>
        Process.GetProcessesByName("Client-Win64-Shipping").Length > 0;

    public static Result Install(string appDirectory, string gameRoot)
    {
        try
        {
            if (IsGameRunning()) return Result.Fail("Hãy đóng game trước khi cài bản Việt hóa R29.");
            if (!PackageVerified(appDirectory))
                return Result.Fail("PAK PC R29 bị thiếu hoặc SHA-256 không khớp. Không ghi vào game.");
            if (CompatibilityIssue(gameRoot) is { } issue)
                return Result.Fail(issue);
            if (IsInstalled(gameRoot)) return Result.Ok();

            var modDir = DestinationDir(gameRoot);
            var mountDir = MountDir(gameRoot);
            var pak = DestinationPak(gameRoot);
            var sig = DestinationSig(gameRoot);
            var sidecar = DestinationMount(gameRoot);
            var originalMount = OfficialMount(gameRoot);
            var sourceSig = StockSig(gameRoot);
            if (string.IsNullOrWhiteSpace(sourceSig) || !File.Exists(sourceSig))
                return Result.Fail("Không tìm thấy tệp SIG ngôn ngữ của game.");
            if (File.Exists(pak) || File.Exists(sig) || File.Exists(sidecar)
                || (Directory.Exists(modDir) && HasReparsePoint(modDir)))
                return Result.Fail("Đã có dữ liệu Mount tên WAHU nhưng chưa xác minh là R29. Không ghi đè.");

            // Never create or change the optional official MountLang_en.txt.
            var mountHashBefore = File.Exists(originalMount)
                ? Sha(originalMount, HashAlgorithmName.SHA256) : null;
            var sourceSigHash = Sha(sourceSig, HashAlgorithmName.SHA256);
            Directory.CreateDirectory(modDir);
            var createdPak = false;
            var createdSig = false;
            var createdSidecar = false;
            try
            {
                createdPak = true;
                File.Copy(Source(appDirectory), pak, overwrite: false);
                if (!ShaMatches(pak, ArchiveSha256)) throw new IOException("PAK copy không khớp hash.");
                createdSig = true;
                File.Copy(sourceSig, sig, overwrite: false);
                if (!ShaMatches(sig, sourceSigHash)) throw new IOException("SIG copy không khớp hash.");
                var content = SidecarText(Sha(pak, HashAlgorithmName.SHA1),
                    Sha(sig, HashAlgorithmName.SHA1));
                createdSidecar = true;
                File.WriteAllText(sidecar, content, new UTF8Encoding(false));
                if (!IsInstalled(gameRoot)) throw new IOException("Hậu kiểm 3 tệp R29 thất bại.");
                if (mountHashBefore is not null
                        ? !ShaMatches(originalMount, mountHashBefore)
                        : File.Exists(originalMount))
                    throw new IOException("Mount chính thức của game đã thay đổi.");
                return Result.Ok();
            }
            catch
            {
                // Paths were absent at preflight. Delete only the files created in this attempt.
                if (createdSidecar && File.Exists(sidecar)) File.Delete(sidecar);
                if (createdSig && File.Exists(sig)) File.Delete(sig);
                if (createdPak && File.Exists(pak)) File.Delete(pak);
                if (Directory.Exists(modDir) && !Directory.EnumerateFileSystemEntries(modDir).Any())
                    Directory.Delete(modDir);
                throw;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or ArgumentException or InvalidDataException)
        {
            return Result.Fail("Không thể cài PAK R29: " + ex.Message, ex);
        }
    }

    public static Result Uninstall(string gameRoot)
    {
        try
        {
            if (IsGameRunning()) return Result.Fail("Hãy đóng game trước khi gỡ bản Việt hóa R29.");
            if (!IsInstalled(gameRoot))
                return Result.Fail("Không xác minh được đúng 3 tệp R29 đã cài. Không xóa file lạ.");

            var mount = DestinationMount(gameRoot);
            var pak = DestinationPak(gameRoot);
            var sig = DestinationSig(gameRoot);
            var modDir = DestinationDir(gameRoot);
            var officialMount = OfficialMount(gameRoot);
            var originalHash = File.Exists(officialMount)
                ? Sha(officialMount, HashAlgorithmName.SHA256) : null;

            // The sidecar is the activation switch. Remove it before the two PAK resources.
            File.Delete(mount);
            File.Delete(sig);
            File.Delete(pak);
            if (!Directory.EnumerateFileSystemEntries(modDir).Any()) Directory.Delete(modDir);
            if (originalHash is not null && !ShaMatches(officialMount, originalHash))
                return Result.Fail("Mount gốc bị thay đổi ngoài phạm vi gỡ R29; hãy kiểm tra game.");
            return Result.Ok();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or ArgumentException or InvalidDataException)
        {
            return Result.Fail("Không thể gỡ PAK R29: " + ex.Message, ex);
        }
    }
}
