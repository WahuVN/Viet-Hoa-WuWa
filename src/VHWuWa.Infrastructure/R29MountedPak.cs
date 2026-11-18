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
    public static string? StockSig(string gameRoot)
    {
        var langEn = Path.Combine(GameResourceDir(gameRoot), "Lang_en");
        if (!Directory.Exists(langEn)) return MountedLanguageFallbackSig(gameRoot);
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
        return baseSig ?? (File.Exists(rootSig) && new FileInfo(rootSig).Length > 0
            ? rootSig : MountedLanguageFallbackSig(gameRoot));
    }

    /// <summary>
    /// Steam users can have only JA/KR/ZH voice resources. When EN SIG is
    /// absent, accept an existing language SIG only if the official
    /// MountLang_*.txt explicitly binds its exact relative PAK path and SHA-1.
    /// Never fabricate a SIG, use arbitrary RoleVoice signatures, or change
    /// any stock MountLang file.
    /// </summary>
    private static string? MountedLanguageFallbackSig(string gameRoot)
    {
        try
        {
            var resources = GameResourceDir(gameRoot);
            var mounts = MountDir(gameRoot);
            if (!Directory.Exists(mounts)) return null;

            foreach (var mountFile in Directory.EnumerateFiles(mounts, "MountLang_*.txt")
                         .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                var language = Path.GetFileNameWithoutExtension(mountFile);
                if (!language.StartsWith("MountLang_", StringComparison.OrdinalIgnoreCase))
                    continue;
                var suffix = language["MountLang_".Length..];
                if (suffix.Length is < 2 or > 8 ||
                    !suffix.All(c => char.IsAsciiLetter(c) || c == '_'))
                    continue;
                var langFolder = "Lang_" + suffix;
                var langRoot = Path.GetFullPath(Path.Combine(resources, langFolder));
                if (!Directory.Exists(langRoot)) continue;
                if (HasReparsePoint(mountFile) || HasReparsePoint(langRoot)) continue;

                var content = File.ReadAllLines(mountFile);
                if (!content.Any(x => x.Trim() == "::Mount::")) continue;
                foreach (var line in content.AsEnumerable().Reverse())
                {
                    var fields = line.Split(',');
                    if (fields.Length < 4) continue;
                    var mountPath = fields[0].Trim().Replace('/', Path.DirectorySeparatorChar);
                    if (!mountPath.StartsWith(langFolder + Path.DirectorySeparatorChar,
                            StringComparison.OrdinalIgnoreCase)) continue;
                    if (!System.Text.RegularExpressions.Regex.IsMatch(
                            mountPath,
                            @"^Lang_[A-Za-z_]{2,8}\\(?:Base|[0-9]+(?:\.[0-9]+)*)\\pakchunk[0-9]+-WindowsNoEditor(?:_P)?$",
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        continue;
                    var expectedSha1 = fields[3].Trim();
                    if (expectedSha1.Length != 40 ||
                        !expectedSha1.All(Uri.IsHexDigit)) continue;
                    var signature = Path.GetFullPath(Path.Combine(resources, mountPath + ".sig"));
                    if (!signature.StartsWith(langRoot + Path.DirectorySeparatorChar,
                            StringComparison.OrdinalIgnoreCase)) continue;
                    if (!File.Exists(signature) || HasReparsePoint(signature) ||
                        new FileInfo(signature).Length == 0) continue;
                    if (Sha(signature, HashAlgorithmName.SHA1)
                        .Equals(expectedSha1, StringComparison.OrdinalIgnoreCase))
                        return signature;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                       or ArgumentException or InvalidDataException)
        {
            return null;
        }
        return null;
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
    /// All releases since 3.0.19 using R29 must never fall back to stale 3.7 loader/PAK files
    /// left behind by the 3.0.18 in-place updater if R29 content is missing.
    /// The release marker is also shipped with the final ZIP.
    /// </summary>
    public static bool VersionRequiresR29(Version? version) =>
        version is { Major: 3 } && (version.Minor > 0 || (version.Minor == 0 && version.Build >= 19));

    public static bool ReleaseRequired(string appDirectory)
    {
        if (File.Exists(Path.Combine(appDirectory, "content", "r29-release.json")))
            return true;
        var entry = System.Reflection.Assembly.GetEntryAssembly()?.GetName();
        return string.Equals(entry?.Name, "VHWuWa", StringComparison.OrdinalIgnoreCase)
            && VersionRequiresR29(entry?.Version);
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


    private static void PrepareForOverwrite(string path)
    {
        if (!File.Exists(path)) return;
        var attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReparsePoint) != 0)
            throw new IOException("Không ghi đè liên kết/reparse point: " + path);
        var cleaned = attributes
            & ~FileAttributes.ReadOnly
            & ~FileAttributes.Hidden
            & ~FileAttributes.System;
        if (cleaned != attributes) File.SetAttributes(path, cleaned);
    }

    private static bool HasUnsafeAncestor(string path, string gameRoot)
    {
        var root = Path.GetFullPath(gameRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var current = Path.GetFullPath(path);
        while (current.StartsWith(root + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase)
               || current.Equals(root, StringComparison.OrdinalIgnoreCase))
        {
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                return true;
            if (current.Equals(root, StringComparison.OrdinalIgnoreCase)) break;
            var parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent)) break;
            current = parent;
        }
        return false;
    }

    public static Result Install(string appDirectory, string gameRoot)
    {
        var staged = new List<string>();
        var old = new List<(string Destination, string Backup)>();
        var installed = new List<string>();
        try
        {
            if (!PackageVerified(appDirectory))
                return Result.Fail("R29 thiếu gói hoặc SHA-256 sai. Không thay đổi file game.");
            var sourceSig = StockSig(gameRoot);
            if (sourceSig is null || !File.Exists(sourceSig))
                return Result.Fail("Không xác minh được SIG game. Chưa ghi file Việt hóa.");
            if (HasUnsafeAncestor(sourceSig, gameRoot))
                return Result.Fail("SIG game nằm trong đường dẫn có liên kết ngoài.");

            var pak = DestinationPak(gameRoot);
            var sig = DestinationSig(gameRoot);
            var sidecar = DestinationMount(gameRoot);
            var destinations = new[] { pak, sig, sidecar };
            if (destinations.Any(p => HasUnsafeAncestor(p, gameRoot)))
                return Result.Fail("Thư mục Việt hóa chứa liên kết ngoài; từ chối ghi đè.");

            // A foreign or partial installation in these same three paths must NOT be
            // overwritten. Only a complete, SHA-verified WAHU R29 install is owned.
            if (destinations.Any(File.Exists) && !IsInstalled(gameRoot))
                return Result.Fail("Đã có tệp tại vị trí R29 nhưng không xác minh được chủ sở hữu. "
                    + "Không ghi đè file lạ; hãy sao lưu và đối chiếu trước.");

            var sourceSigHash = Sha(sourceSig, HashAlgorithmName.SHA256);
            Directory.CreateDirectory(DestinationDir(gameRoot));
            Directory.CreateDirectory(MountDir(gameRoot));

            var suffix = ".wahu-" + Guid.NewGuid().ToString("N");
            var stagePaths = destinations.Select(p => p + suffix + ".new").ToArray();
            staged.AddRange(stagePaths);
            File.Copy(Source(appDirectory), stagePaths[0], overwrite: false);
            File.Copy(sourceSig, stagePaths[1], overwrite: false);
            if (!ShaMatches(stagePaths[0], ArchiveSha256)
                || !Sha(stagePaths[1], HashAlgorithmName.SHA256).Equals(sourceSigHash,
                    StringComparison.OrdinalIgnoreCase))
                throw new IOException("PAK hoặc SIG tạm không khớp SHA-256.");
            File.WriteAllText(stagePaths[2],
                SidecarText(Sha(stagePaths[0], HashAlgorithmName.SHA1),
                    Sha(stagePaths[1], HashAlgorithmName.SHA1)),
                new UTF8Encoding(false));

            // Stage fully first; move verified WAHU-owned files aside before replacing.
            for (var i = 0; i < destinations.Length; i++)
            {
                if (!File.Exists(destinations[i])) continue;
                var backup = destinations[i] + suffix + ".old";
                File.Move(destinations[i], backup);
                old.Add((destinations[i], backup));
            }
            for (var i = 0; i < destinations.Length; i++)
            {
                File.Move(stagePaths[i], destinations[i]);
                installed.Add(destinations[i]);
            }
            if (!IsInstalled(gameRoot))
                throw new IOException("Kiểm tra bản dịch sau khi cài không khớp SHA.");

            // Delete only backups of the R29 files we already verified as ours.
            foreach (var (_, backup) in old)
                try { if (File.Exists(backup)) File.Delete(backup); }
                catch (IOException) { /* keep backup for manual cleanup */ }
                catch (UnauthorizedAccessException) { /* retain recoverable backup */ }
            return Result.Ok();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                   or ArgumentException or InvalidDataException)
        {
            foreach (var file in installed.AsEnumerable().Reverse())
                try { if (File.Exists(file)) File.Delete(file); } catch { }
            foreach (var (destination, backup) in old.AsEnumerable().Reverse())
                try
                {
                    if (File.Exists(backup) && !File.Exists(destination))
                        File.Move(backup, destination);
                }
                catch { /* leave .old file for recovery; do not overwrite any file */ }
            return Result.Fail("Cài PAK R29 thất bại, đã thử khôi phục: " + ex.Message, ex);
        }
        finally
        {
            foreach (var file in staged)
                try { if (File.Exists(file)) File.Delete(file); } catch { }
        }
    }

    public static Result Uninstall(string gameRoot)
    {
        try
        {
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
