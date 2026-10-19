using System.Text.Json;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;

namespace VHWuWa.Infrastructure;

/// <summary>
/// Cài Việt hóa trực tiếp từ thư mục <c>content\</c> đi kèm app — GIỐNG HỆT cách tool dịch Wahu cài
/// (bộ cài WuwaVH_BanCai):
///   • Pak + font  ->  <c>&lt;game&gt;\Client\Content\Paks\~WuWaMods\</c>  (game tự quét &amp; mount)
///   • Mỗi pak kèm 1 file <c>.sig</c> (copy từ .sig gốc của game, hoặc tạo placeholder rỗng)
///   • Loader ->  <c>Win64\</c>: <c>version.dll</c>, <c>verorg.dll</c>, <c>WuWaVH.dll</c> (backup version.dll gốc)
/// Hỗ trợ cả bản Launcher (Kuro) lẫn Steam vì chỉ dựa vào đường dẫn tương đối Client\.
/// </summary>
public sealed class ViethoaInstaller : IViethoaInstaller
{
    private const string PakName = "WuWaVH_99_P.pak";        // tên pak bản dịch trong ~WuWaMods
    private const string ModsFolder = "~WuWaMods";
    private const string MarkerName = "vhwuwa_install.json";
    private static readonly HashSet<string> ModExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pak", ".sig", ".utoc", ".ucas" };
    private static readonly string[] ProxyLoaders =
        { "version.dll", "dxgi.dll", "d3d11.dll", "dinput8.dll", "winmm.dll", "xinput1_3.dll", "xinput1_4.dll", "dsound.dll", "winhttp.dll" };
    private static readonly string[] KnownForeignWin64Files =
        {
            "d3dx.ini", "3DMigoto Loader.exe",
            // Artifact Việt hóa/mod ngoài từng thấy trong workspace/tài liệu nghiên cứu; không phải file managed của VHWuWa.
            "wuwaVietHoa.dll", "wuwaVietHoa_Wahu_SDK.dll", "wuwaVietHoa_SDK.dll",
            "wuwaVietHoa_VH.dll", "wuwaVietHoa.off", "SigPakV2.dll",
            // Mod camera/injector từng dùng chung cơ chế proxy version.dll.
            "camera.dll", "photoCloseUp.dll", "photoCloseUp_full.dll", "injectdll.exe",
            // Backup files are deliberately excluded from automatic deletion.
        };
    private static readonly string[] KnownForeignWin64Dirs =
        { "Mods", "WWMI", "3Dmigoto", "ShaderFixes", "ShaderCache", "ue4ss", "RE-UE4SS", "wuwaVietHoa", "wuwaIndonesia" };
    // Latest quarantine is retained for the UI even when cleanup was triggered automatically.
    public string? LastQuarantineDirectory { get; private set; }
    public int LastAutoDeletedConflictCount { get; private set; }
    private readonly ILogService _log;
    private readonly string _contentDir;
    private readonly string _quarantineRoot;
    private readonly Func<bool> _isGameRunning;

    private sealed class OperationLease : IDisposable
    {
        private Semaphore? _semaphore;

        public OperationLease(Semaphore semaphore) => _semaphore = semaphore;

        public void Dispose()
        {
            var semaphore = Interlocked.Exchange(ref _semaphore, null);
            if (semaphore is null) return;
            try { semaphore.Release(); }
            finally { semaphore.Dispose(); }
        }
    }

    public ViethoaInstaller(ILogService log, string? contentDir = null, string? quarantineRoot = null,
        Func<bool>? isGameRunning = null)
    {
        _log = log;
        _contentDir = contentDir ?? Path.Combine(AppContext.BaseDirectory, "content");
        _quarantineRoot = quarantineRoot ?? Path.Combine(AppPaths.AppData, "Quarantine");
        _isGameRunning = isGameRunning ?? DetectGameRunning;
    }

    private string HanVietPak => Path.Combine(_contentDir, "WuWaVH_HanViet_99_P.pak");
    private string EnPak => Path.Combine(_contentDir, "WuWaVH_EN_99_P.pak");
    private string LoaderDir => Path.Combine(_contentDir, "loader");

    private string? FindFontPak()
    {
        var fdir = Path.Combine(_contentDir, "font");
        if (!Directory.Exists(fdir)) return null;
        var preferred = Path.Combine(fdir, "WahuFont_100_P.pak");
        if (File.Exists(preferred)) return preferred;
        return Directory.EnumerateFiles(fdir, "*.pak")
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public ViethoaContent InspectContent()
    {
        var c = new ViethoaContent { ContentDir = _contentDir };
        c.HasHanViet = File.Exists(HanVietPak);
        c.HasEnglish = File.Exists(EnPak);
        c.HasLoader = File.Exists(Path.Combine(LoaderDir, "version.dll"))
                   && File.Exists(Path.Combine(LoaderDir, "verorg.dll"))
                   && File.Exists(Path.Combine(LoaderDir, "WuWaVH.dll"));
        c.FontPak = FindFontPak();
        return c;
    }

    private static string Win64Of(string gamePath) =>
        Path.Combine(gamePath, "Client", "Binaries", "Win64");

    // Official DangDevVH 2.3.0 installs winhttp.dll and Win64\wuwaVietHoa.
    // An older public fork uses Win64\wuwaIndonesia. Detect both before cleanup.
    private static bool HasForeignWinHttpPakLoader(string gamePath)
    {
        var win64 = Win64Of(gamePath);
        foreach (var name in new[] { "wuwaVietHoa", "wuwaIndonesia" })
        {
            var path = Path.Combine(win64, name);
            if (Directory.Exists(path) && (IsLinkedDirectory(path) || DirectoryHasForeignFiles(gamePath, path)))
                return true;
        }

        // A partial DangDev installation may retain only the official DLL.
        // Match the immutable SHA-256 from the publisher's Wuwa/dlls/winhttp.dll.
        var loader = Path.Combine(win64, "winhttp.dll");
        if (!File.Exists(loader)) return false;
        try
        {
            using var stream = File.OpenRead(loader);
            var hash = Convert.ToHexString(SHA256.HashData(stream));
            return hash.Equals("E757622BB86041860CEA04EB164D298C21B5AC3565EF7B44C0D6126939F4E083",
                StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }

    private static string ForeignWinHttpInstallWarning =>
        "Phát hiện dấu vết bộ Việt hoá khác (Win64\\wuwaVietHoa, wuwaIndonesia "
        + "hoặc winhttp.dll của Đặng Dev). Không tự dọn hoặc cài chồng lên WAHU "
        + "vì có thể nạp hai bộ PAK cùng lúc. Chưa xóa hoặc ghi đè file nào. "
        + "Hãy quét mod, xác minh nguồn file và gỡ bộ không dùng trước khi cài.";
    private static string PaksOf(string gamePath) =>
        Path.Combine(gamePath, "Client", "Content", "Paks");
    private static string ModsOf(string gamePath) =>
        Path.Combine(PaksOf(gamePath), ModsFolder);

    private static bool? LatestOfficialManifestContains(string gamePath, string destination)
    {
        try
        {
            var launcherDownload = Path.Combine(gamePath, "launcherDownload");
            if (!Directory.Exists(launcherDownload)) return null;
            var manifest = Directory.EnumerateFiles(launcherDownload, "OriginResource.json", SearchOption.AllDirectories)
                .Select(path =>
                {
                    var folder = Path.GetFileName(Path.GetDirectoryName(path));
                    var parsed = Version.TryParse(folder, out var version) ? version : null;
                    return new { Path = path, Version = parsed, LastWrite = File.GetLastWriteTimeUtc(path) };
                })
                .OrderByDescending(x => x.Version is not null)
                .ThenByDescending(x => x.Version)
                .ThenByDescending(x => x.LastWrite)
                .Select(x => x.Path)
                .FirstOrDefault();
            if (manifest is null) return null;

            using var stream = File.OpenRead(manifest);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("resource", out var resources)
                || resources.ValueKind != JsonValueKind.Array)
                return null;

            var expected = destination.Replace('\\', '/');
            foreach (var item in resources.EnumerateArray())
            {
                if (!item.TryGetProperty("dest", out var dest) || dest.ValueKind != JsonValueKind.String)
                    continue;
                if (string.Equals(dest.GetString()?.Replace('\\', '/'), expected,
                        StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsOfficialGameFile(string gamePath, string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return false;
            var relative = Path.GetRelativePath(gamePath, filePath).Replace('\\', '/');
            if (relative.StartsWith("../", StringComparison.Ordinal)
                || relative.Equals("..", StringComparison.Ordinal))
                return false;
            return LatestOfficialManifestContains(gamePath, relative) == true;
        }
        catch
        {
            return false;
        }
    }

    private static bool DirectoryHasForeignFiles(string gamePath, string directory)
    {
        if (!Directory.Exists(directory)) return false;
        return SafeEnumerateFiles(directory).Any(file =>
            !IsProtectedBackupFile(file) && !IsOfficialGameFile(gamePath, file));
    }

    private static bool IsStaleVersionBackup(string gamePath)
    {
        var backup = Path.Combine(Win64Of(gamePath), "version_goc.dll");
        return File.Exists(backup)
               && LatestOfficialManifestContains(gamePath, "Client/Binaries/Win64/version.dll") != true;
    }

    private static bool DetectGameRunning()
    {
        try
        {
            var processes = Process.GetProcessesByName("Client-Win64-Shipping")
                .Concat(Process.GetProcessesByName("Wuthering Waves"))
                .Concat(Process.GetProcessesByName("launcher_main"))
                .ToArray();
            try { return processes.Length > 0; }
            finally { foreach (var process in processes) process.Dispose(); }
        }
        catch { return false; }
    }

    private bool IsGameRunning() => _isGameRunning();

    private static string CurrentPackageVersion()
    {
        var version = typeof(ViethoaInstaller).Assembly.GetName().Version;
        return version is null ? "" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static OperationLease? TryAcquireOperationLock(string gamePath)
    {
        var normalized = Path.GetFullPath(gamePath).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))[..20];
        var semaphore = new Semaphore(1, 1, @"Local\VHWuWa.Install." + id);
        try
        {
            if (semaphore.WaitOne(0)) return new OperationLease(semaphore);
        }
        catch
        {
            semaphore.Dispose();
            throw;
        }
        semaphore.Dispose();
        return null;
    }

    public ViethoaStatus GetStatus(string gamePath)
    {
        var st = new ViethoaStatus { GamePath = gamePath };
        try
        {
            var mods = ModsOf(gamePath);
            var win64 = Win64Of(gamePath);
            var marker = ViethoaInstallMarker.Load(Path.Combine(mods, MarkerName));
            var hasWahuFiles = File.Exists(Path.Combine(mods, PakName))
                || File.Exists(Path.Combine(win64, "WuWaVH.dll"))
                || File.Exists(Path.Combine(win64, "verorg.dll"));
            // A single foreign DLL using the same filename does not prove that
            // VHWuWa is installed. Require a valid marker or a complete legacy set.
            st.Installed = (marker is not null && hasWahuFiles)
                || IsLegacyVhwuwaBundle(gamePath, marker);
            if (marker is not null)
            {
                st.Version = string.IsNullOrWhiteSpace(marker.PackageVersion)
                    ? CurrentPackageVersion()
                    : marker.PackageVersion;
                st.Variant = marker.Variant;
                st.FontPak = marker.Font;
            }
            else if (st.Installed) st.Version = ""; // Legacy chưa có marker: không giả là phiên bản hiện tại.
        }
        catch (Exception ex) { _log.Warn("Viethoa", "Đọc trạng thái lỗi: " + ex.Message); }
        return st;
    }

    private static bool IsLegacyVhwuwaBundle(string gamePath, ViethoaInstallMarker? marker)
    {
        // Các bản VHWuWa cũ có thể chưa có marker v2. Chỉ coi là legacy khi
        // thấy đủ bộ file đặc trưng của chính VHWuWa, tránh bỏ qua mod ngoài
        // chỉ vì trùng một tên file đơn lẻ.
        if (marker is not null) return false;
        var mods = ModsOf(gamePath);
        var win64 = Win64Of(gamePath);
        return File.Exists(Path.Combine(mods, PakName))
            && File.Exists(Path.Combine(win64, "version.dll"))
            && File.Exists(Path.Combine(win64, "verorg.dll"))
            && File.Exists(Path.Combine(win64, "WuWaVH.dll"));
    }

    private static bool IsLegacyVhwuwaModFile(string filePath, bool legacyBundle)
    {
        if (!legacyBundle) return false;
        var name = Path.GetFileName(filePath);
        return name.Equals(PakName, StringComparison.OrdinalIgnoreCase)
            || name.Equals(Path.ChangeExtension(PakName, ".sig"), StringComparison.OrdinalIgnoreCase)
            || name.Equals("WahuFont_100_P.pak", StringComparison.OrdinalIgnoreCase)
            || name.Equals("WahuFont_100_P.sig", StringComparison.OrdinalIgnoreCase);
    }

    private HashSet<string> OwnedModFiles(string mods)
    {
        var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PakName, Path.ChangeExtension(PakName, ".sig"), MarkerName
        };

        var marker = ViethoaInstallMarker.Load(Path.Combine(mods, MarkerName));
        if (!string.IsNullOrWhiteSpace(marker?.Font))
        {
            var name = Path.GetFileName(marker.Font);
            owned.Add(name);
            owned.Add(Path.ChangeExtension(name, ".sig"));
        }
        if (marker is not null)
        {
            foreach (var key in marker.ManagedFiles.Keys)
            {
                if (!key.StartsWith("mods/", StringComparison.OrdinalIgnoreCase)) continue;
                owned.Add(Path.GetFileName(key));
            }
        }
        return owned;
    }

    private bool MatchesLegacyManagedFile(ViethoaInstallMarker? marker, string scope, string filePath)
    {
        if (!File.Exists(filePath)) return true;
        if (marker is not null)
        {
            var key = scope.Equals("win64", StringComparison.OrdinalIgnoreCase)
                ? ViethoaInstallMarker.LoaderKey(Path.GetFileName(filePath))
                : ViethoaInstallMarker.ModKey(Path.GetFileName(filePath));
            if (marker.ManagedFiles.ContainsKey(key)) return marker.Matches(scope, filePath);
        }

        var name = Path.GetFileName(filePath);
        if (scope.Equals("win64", StringComparison.OrdinalIgnoreCase))
        {
            var source = Path.Combine(LoaderDir, name);
            return File.Exists(source) && SameFile(filePath, source);
        }
        if (name.Equals(PakName, StringComparison.OrdinalIgnoreCase))
        {
            var source = marker?.Variant == "en" ? EnPak : HanVietPak;
            return File.Exists(source) && SameFile(filePath, source);
        }
        if (!string.IsNullOrWhiteSpace(marker?.Font)
            && name.Equals(Path.GetFileName(marker.Font), StringComparison.OrdinalIgnoreCase))
        {
            var bundled = FindFontPak();
            return bundled is null || !Path.GetFileName(bundled).Equals(name, StringComparison.OrdinalIgnoreCase)
                || SameFile(filePath, bundled);
        }
        return true;
    }

    private IReadOnlyList<string> FindManagedFileChanges(string gamePath)
    {
        var changes = new List<string>();
        var mods = ModsOf(gamePath);
        var win64 = Win64Of(gamePath);
        var marker = ViethoaInstallMarker.Load(Path.Combine(mods, MarkerName));

        foreach (var name in OwnedModFiles(mods).Where(n => !n.Equals(MarkerName, StringComparison.OrdinalIgnoreCase)))
        {
            var path = Path.Combine(mods, name);
            if (File.Exists(path) && !MatchesLegacyManagedFile(marker, "mods", path))
                changes.Add("File Việt hóa/font đã bị thay đổi: " + Path.Combine(ModsFolder, name));
        }
        foreach (var name in new[] { "version.dll", "verorg.dll", "WuWaVH.dll" })
        {
            var path = Path.Combine(win64, name);
            if (File.Exists(path) && !MatchesLegacyManagedFile(marker, "win64", path))
                changes.Add("Loader Việt hóa đã bị mod khác ghi đè: Win64\\" + name);
        }
        return changes;
    }

    private static bool SameFile(string left, string right)
    {
        try
        {
            if (!File.Exists(left) || !File.Exists(right)) return false;
            var a = new FileInfo(left); var b = new FileInfo(right);
            if (a.Length != b.Length) return false;
            using var x = File.OpenRead(left); using var y = File.OpenRead(right);
            return SHA256.HashData(x).SequenceEqual(SHA256.HashData(y));
        }
        catch { return false; }
    }

    private IReadOnlyList<string> VerifyInstallation(string gamePath, string sourcePak,
        NameVariant variant, string? sourceFont, string? fontName)
    {
        var errors = new List<string>();
        var mods = ModsOf(gamePath);
        var win64 = Win64Of(gamePath);

        static void CheckCopy(List<string> output, string label, string source, string destination)
        {
            if (!File.Exists(destination))
                output.Add($"Thiếu {label}: {destination}");
            else if (!SameFile(source, destination))
                output.Add($"Sai dung lượng hoặc SHA-256 của {label}: {destination}");
        }

        CheckCopy(errors, "PAK bản dịch", sourcePak, Path.Combine(mods, PakName));
        if (!File.Exists(Path.Combine(mods, Path.ChangeExtension(PakName, ".sig"))))
            errors.Add("Thiếu chữ ký PAK: " + Path.ChangeExtension(PakName, ".sig"));

        if (sourceFont is not null && fontName is not null)
        {
            CheckCopy(errors, "font", sourceFont, Path.Combine(mods, fontName));
            if (!File.Exists(Path.Combine(mods, Path.ChangeExtension(fontName, ".sig"))))
                errors.Add("Thiếu chữ ký font: " + Path.ChangeExtension(fontName, ".sig"));
        }

        foreach (var dll in new[] { "version.dll", "verorg.dll", "WuWaVH.dll" })
            CheckCopy(errors, "loader " + dll, Path.Combine(LoaderDir, dll), Path.Combine(win64, dll));

        var markerPath = Path.Combine(mods, MarkerName);
        if (!File.Exists(markerPath))
            errors.Add("Thiếu file xác nhận cài đặt: " + MarkerName);
        else
        {
            try
            {
                using var marker = JsonDocument.Parse(File.ReadAllText(markerPath));
                var expectedVariant = variant == NameVariant.English ? "en" : "hanviet";
                var actualVariant = marker.RootElement.TryGetProperty("variant", out var value)
                    ? value.GetString() : null;
                if (!string.Equals(expectedVariant, actualVariant, StringComparison.Ordinal))
                    errors.Add($"Marker sai biến thể: cần {expectedVariant}, nhận {actualVariant ?? "(trống)"}");
                var actualFont = marker.RootElement.TryGetProperty("font", out var font)
                    && font.ValueKind != JsonValueKind.Null ? font.GetString() : null;
                if (!string.Equals(fontName, actualFont, StringComparison.OrdinalIgnoreCase))
                    errors.Add($"Marker sai font: cần {fontName ?? "(không cài)"}, nhận {actualFont ?? "(không cài)"}");
            }
            catch (Exception ex)
            {
                errors.Add("File xác nhận cài đặt không đọc được: " + ex.Message);
            }
        }
        // Read the persisted marker again: existence alone is insufficient.
        // Every managed PAK/SIG/DLL must have a matching SHA-256 entry.
        if (File.Exists(markerPath))
        {
            var attestation = ViethoaInstallMarker.Load(markerPath);
            if (attestation is null)
                errors.Add("Marker quản lý file không đọc được hoặc bị hỏng.");
            else
            {
                var expected = new List<(string Scope, string Path)>
                {
                    ("mods", Path.Combine(mods, PakName)),
                    ("mods", Path.Combine(mods, Path.ChangeExtension(PakName, ".sig"))),
                    ("win64", Path.Combine(win64, "version.dll")),
                    ("win64", Path.Combine(win64, "verorg.dll")),
                    ("win64", Path.Combine(win64, "WuWaVH.dll"))
                };
                if (fontName is not null)
                {
                    expected.Add(("mods", Path.Combine(mods, fontName)));
                    expected.Add(("mods", Path.Combine(mods, Path.ChangeExtension(fontName, ".sig"))));
                }
                foreach (var (scope, installedFile) in expected)
                {
                    var key = scope == "mods"
                        ? ViethoaInstallMarker.ModKey(Path.GetFileName(installedFile))
                        : ViethoaInstallMarker.LoaderKey(Path.GetFileName(installedFile));
                    if (!attestation.ManagedFiles.TryGetValue(key, out var sha))
                    {
                        errors.Add("Marker thiếu SHA-256 cho file: " + key);
                        continue;
                    }
                    try
                    {
                        if (!string.Equals(sha, ViethoaInstallMarker.Sha256(installedFile),
                            StringComparison.OrdinalIgnoreCase))
                            errors.Add("SHA-256 file cài không khớp marker: " + key);
                    }
                    catch (Exception ex)
                    {
                        errors.Add("Không thể kiểm tra SHA-256 " + key + ": " + ex.Message);
                    }
                }
            }
        }
        return errors;
    }

    private static IEnumerable<string> EnumerateModFolders(string paks) =>
        Directory.EnumerateDirectories(paks, "*", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetFileName(path).StartsWith("~", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(path).Equals("Mods", StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(path).Equals("_mods", StringComparison.OrdinalIgnoreCase));

    private static void EnsureManagedMarkerReadable(string mods)
    {
        var markerPath = Path.Combine(mods, MarkerName);
        if (File.Exists(markerPath) && ViethoaInstallMarker.Load(markerPath) is null)
            throw new InvalidDataException("File vhwuwa_install.json bị hỏng hoặc không đọc được. Không tự động xóa mod để tránh mất file WAHU; hãy sao lưu thư mục ~WuWaMods và kiểm tra marker trước.");
    }

    private static bool IsReservedWahuModName(string name) =>
        name.Equals(PakName, StringComparison.OrdinalIgnoreCase)
        || name.Equals(Path.ChangeExtension(PakName, ".sig"), StringComparison.OrdinalIgnoreCase)
        || name.Equals(MarkerName, StringComparison.OrdinalIgnoreCase);

    private static bool IsLinkedDirectory(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch { return true; }
    }

    // Backups never participate in mod auto-clean or permanent-delete, even when
    // located inside third-party mod folders. version_goc.dll can be the sole
    // recoverable copy of a pre-install loader and is not itself an active injector.
    private static bool IsProtectedBackupFile(string path)
    {
        var name = Path.GetFileName(path);
        return name.Equals("version_goc.dll", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".backup", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".bak_wuwa", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".orig", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".old", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Dò mod phổ biến nhưng tránh quét nhầm pakchunk chính thức ở gốc Paks.</summary>
    public IReadOnlyList<string> FindConflicts(string gamePath)
    {
        var conflicts = new List<string>();
        try
        {
            var paks = PaksOf(gamePath);
            var mods = ModsOf(gamePath);
            EnsureManagedMarkerReadable(mods);
            var markerExists = File.Exists(Path.Combine(mods, MarkerName));
            var marker = ViethoaInstallMarker.Load(Path.Combine(mods, MarkerName));
            var legacyBundle = IsLegacyVhwuwaBundle(gamePath, marker);
            var owned = OwnedModFiles(mods);

            if (Directory.Exists(paks))
            {
                // Một số mod được thả trực tiếp vào Paks thay vì thư mục ~mods.
                // Chỉ nhận mẫu *_P.* và loại pakchunk chính thức để tránh báo giả hàng nghìn file game.
                foreach (var file in Directory.EnumerateFiles(paks, "*", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(file);
                    var extension = Path.GetExtension(file);
                    if (!ModExtensions.Contains(extension)
                        || IsProtectedBackupFile(file)
                        || name.StartsWith("pakchunk", StringComparison.OrdinalIgnoreCase)
                        || !Path.GetFileNameWithoutExtension(name).EndsWith("_P", StringComparison.OrdinalIgnoreCase)
                        || IsOfficialGameFile(gamePath, file))
                        continue;
                    if (extension.Equals(".sig", StringComparison.OrdinalIgnoreCase)
                        && File.Exists(Path.ChangeExtension(file, ".pak"))) continue;
                    conflicts.Add("Mod ngoài ở thư mục Paks: " + name);
                }

                foreach (var dir in EnumerateModFolders(paks))
                {
                    if (IsLinkedDirectory(dir))
                    {
                        conflicts.Add("Thư mục mod liên kết cần kiểm tra thủ công: " + Path.GetRelativePath(paks, dir));
                        continue;
                    }
                    var isOwnDirectory = Path.GetFullPath(dir)
                        .Equals(Path.GetFullPath(mods), StringComparison.OrdinalIgnoreCase);
                    foreach (var file in SafeEnumerateFiles(dir))
                    {
                        if (IsProtectedBackupFile(file) || IsOfficialGameFile(gamePath, file)) continue;
                        var isTopLevel = Path.GetDirectoryName(file)!
                            .Equals(dir, StringComparison.OrdinalIgnoreCase);
                        var name = Path.GetFileName(file);

                        if (isOwnDirectory && isTopLevel)
                        {
                            // A missing marker is not evidence that reserved WAHU files are foreign.
                            // The installer will overwrite and track these names after cleanup.
                            if (marker is null && IsReservedWahuModName(name)) continue;
                            if (IsLegacyVhwuwaModFile(file, legacyBundle)) continue;
                            if (markerExists && owned.Contains(name))
                            {
                                if (!MatchesLegacyManagedFile(marker, "mods", file))
                                    conflicts.Add("File của Việt hóa đã bị mod khác ghi đè: "
                                        + Path.GetRelativePath(paks, file));
                                continue;
                            }
                        }

                        // A signature belonging to a listed PAK is one conflict group in the UI.
                        // Both files are still copied to quarantine by CollectConflictFiles.
                        if (Path.GetExtension(file).Equals(".sig", StringComparison.OrdinalIgnoreCase)
                            && File.Exists(Path.ChangeExtension(file, ".pak"))) continue;

                        // Mọi file còn lại trong thư mục mod ~* đều là ngoài phạm vi
                        // managed của VHWuWa, kể cả .json/.ini/.txt phụ của mod cũ.
                        conflicts.Add((isOwnDirectory ? "Mod khác: " : "Thư mục mod khác: ")
                            + Path.GetRelativePath(paks, file));
                    }
                }
            }

            var win64 = Win64Of(gamePath);
            foreach (var name in ProxyLoaders)
            {
                var installed = Path.Combine(win64, name);
                if (!File.Exists(installed) || IsOfficialGameFile(gamePath, installed)) continue;
                if (legacyBundle && name.Equals("version.dll", StringComparison.OrdinalIgnoreCase)) continue;
                var ownSource = Path.Combine(LoaderDir, name);
                if (markerExists && File.Exists(ownSource)
                    && MatchesLegacyManagedFile(marker, "win64", installed)) continue;
                conflicts.Add("Loader/proxy khác: Win64\\" + name);
            }
            foreach (var name in new[] { "verorg.dll", "WuWaVH.dll" })
            {
                var installed = Path.Combine(win64, name);
                if (markerExists && File.Exists(installed)
                    && !MatchesLegacyManagedFile(marker, "win64", installed))
                    conflicts.Add("Loader Việt hóa đã bị mod khác ghi đè: Win64\\" + name);
            }
            foreach (var name in KnownForeignWin64Files)
            {
                var installed = Path.Combine(win64, name);
                if (File.Exists(installed) && !IsProtectedBackupFile(installed)
                    && !IsOfficialGameFile(gamePath, installed))
                    conflicts.Add("File mod ngoài: Win64\\" + name);
            }
            // Backup files are deliberately omitted from conflict scan.
            foreach (var dirName in KnownForeignWin64Dirs)
            {
                var dir = Path.Combine(win64, dirName);
                if (DirectoryHasForeignFiles(gamePath, dir))
                    conflicts.Add("Bộ nạp/mod khác: Win64\\" + dirName + "\\");
            }
        }
        catch (Exception ex)
        {
            conflicts.Add("Không thể kiểm tra mod: " + ex.Message);
        }
        return conflicts.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    }

    public Result<ModQuarantineReport> QuarantineConflicts(string gamePath) =>
        QuarantineConflictsCore(gamePath, acquireLock: true);

    // Restricted old-translation cleanup: do not touch a WAHU-managed file, a
    // game file, another WinHTTP proxy, or an unrelated mod directory.
    private IReadOnlyList<string> CollectOldTranslationFiles(string gamePath)
    {
        var win64 = Win64Of(gamePath);
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in new[] { "wuwaVietHoa", "wuwaIndonesia" })
        {
            var dir = Path.Combine(win64, folder);
            if (IsLinkedDirectory(dir)) continue;
            foreach (var file in SafeEnumerateFiles(dir))
            {
                if (!IsProtectedBackupFile(file) && !IsOfficialGameFile(gamePath, file))
                    found.Add(Path.GetFullPath(file));
            }
        }

        var winhttp = Path.Combine(win64, "winhttp.dll");
        if (File.Exists(winhttp) && !IsOfficialGameFile(gamePath, winhttp))
        {
            // Only quarantine the byte-for-byte known official DangDev loader.
            // Unknown WinHTTP proxies must be reviewed manually.
            try
            {
                using var source = File.OpenRead(winhttp);
                if (Convert.ToHexString(SHA256.HashData(source)).Equals(
                    "E757622BB86041860CEA04EB164D298C21B5AC3565EF7B44C0D6126939F4E083",
                    StringComparison.OrdinalIgnoreCase))
                    found.Add(Path.GetFullPath(winhttp));
            }
            catch (IOException) { /* deny automatic removal if file cannot be verified */ }
            catch (UnauthorizedAccessException) { /* deny automatic removal */ }
        }

        return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public IReadOnlyList<string> FindOldTranslationConflicts(string gamePath)
    {
        try
        {
            var root = Path.GetFullPath(gamePath);
            return CollectOldTranslationFiles(root)
                .Select(file => Path.GetRelativePath(root, file)).ToList();
        }
        catch (Exception ex)
        {
            return new[] { "Không thể kiểm tra mod: " + ex.Message };
        }
    }

    public Result<ModQuarantineReport> QuarantineOldTranslationConflicts(string gamePath) =>
        QuarantineConflictsCore(gamePath, acquireLock: true, oldTranslationOnly: true);

    private Result<ModQuarantineReport> QuarantineConflictsCore(string gamePath, bool acquireLock,
        bool oldTranslationOnly = false)
    {
        var moved = new List<(string Source, string Target, string Relative, string Hash)>();
        try
        {
            if (IsGameRunning())
                return Result<ModQuarantineReport>.Fail("Game hoặc launcher đang chạy. Hãy đóng hẳn trước khi dọn mod xung đột.");
            if (oldTranslationOnly && (!File.Exists(Path.Combine(Win64Of(gamePath), "Client-Win64-Shipping.exe"))
                || !Directory.Exists(PaksOf(gamePath))))
                return Result<ModQuarantineReport>.Fail("Hãy chọn đúng thư mục gốc game (có Client\\Binaries\\Win64 và Client\\Content\\Paks).");

            using var operationLock = acquireLock ? TryAcquireOperationLock(gamePath) : null;
            if (acquireLock && operationLock is null)
                return Result<ModQuarantineReport>.Fail("Đang có một tiến trình VHWuWa khác cài/gỡ. Hãy chờ hoàn tất.");

            var targets = oldTranslationOnly ? CollectOldTranslationFiles(gamePath) : CollectConflictFiles(gamePath);
            if (targets.Count == 0)
                return Result<ModQuarantineReport>.Fail(oldTranslationOnly
                    ? "Không tìm thấy file Đặng Dev/fork đã xác minh có thể dọn an toàn. Loader không rõ nguồn cần kiểm tra thủ công."
                    : "Không tìm thấy file mod xung đột có thể cách ly an toàn.");

            // Validate every source BEFORE moving the first one, including symlinks
            // and file locks. A linked file could otherwise read from outside game.
            var gameRoot = Path.GetFullPath(gamePath);
            foreach (var source in targets)
            {
                var relative = Path.GetRelativePath(gameRoot, source);
                if (relative.StartsWith("..", StringComparison.Ordinal))
                    throw new InvalidOperationException("Mod nằm ngoài thư mục game: " + source);
                if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Không cách ly symbolic link/reparse point: " + source);
                using var probe = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None);
            }

            var quarantine = Path.Combine(_quarantineRoot,
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + Guid.NewGuid().ToString("N")[..6]);
            Directory.CreateDirectory(quarantine);
            foreach (var source in targets)
            {
                var relative = Path.GetRelativePath(gamePath, source);
                if (relative.StartsWith("..", StringComparison.Ordinal))
                    throw new InvalidOperationException("Đường dẫn mod nằm ngoài thư mục game: " + source);
                var target = PathValidation.ResolveInsideRoot(quarantine, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                var hash = ViethoaInstallMarker.Sha256(source);
                File.Copy(source, target, overwrite: false);
                if (!hash.Equals(ViethoaInstallMarker.Sha256(target), StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(target);
                    throw new IOException("File cách ly sai SHA-256: " + relative);
                }
                File.Delete(source);
                moved.Add((source, target, relative, hash));
            }

            var manifest = new
            {
                schemaVersion = 1,
                gamePath = Path.GetFullPath(gamePath),
                quarantinedAt = DateTimeOffset.Now,
                files = moved.Select(x => new { originalPath = x.Relative, sha256 = x.Hash }).ToList()
            };
            File.WriteAllText(Path.Combine(quarantine, "quarantine-manifest.json"),
                JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));

            RemoveEmptyModDirectories(gamePath);
            var report = new ModQuarantineReport
            {
                QuarantineDirectory = quarantine,
                MovedFiles = moved.Select(x => x.Relative).ToList()
            };
            LastQuarantineDirectory = quarantine;
            _log.Info("Conflict", $"Đã cách ly {moved.Count} file mod xung đột vào {quarantine}");
            return Result<ModQuarantineReport>.Ok(report);
        }
        catch (Exception ex)
        {
            foreach (var item in moved.AsEnumerable().Reverse())
            {
                try
                {
                    if (!File.Exists(item.Target)) continue;
                    Directory.CreateDirectory(Path.GetDirectoryName(item.Source)!);
                    File.Copy(item.Target, item.Source, overwrite: true);
                    if (item.Hash.Equals(ViethoaInstallMarker.Sha256(item.Source), StringComparison.OrdinalIgnoreCase))
                        File.Delete(item.Target);
                }
                catch { }
            }
            _log.Error("Conflict", "Cách ly mod thất bại: " + ex.Message, ex);
            return Result<ModQuarantineReport>.Fail("Cách ly mod thất bại; các file đã di chuyển được hoàn tác: " + ex.Message, ex);
        }
    }

    public Result<int> DeleteConflicts(string gamePath)
    {
        try
        {
            if (IsGameRunning())
                return Result<int>.Fail("Game hoặc launcher đang chạy. Hãy đóng hẳn trước khi xóa mod xung đột.");

            using var operationLock = TryAcquireOperationLock(gamePath);
            if (operationLock is null)
                return Result<int>.Fail("Đang có một tiến trình VHWuWa khác cài/gỡ. Hãy chờ hoàn tất.");

            return DeleteConflictsUnderLock(gamePath, failWhenEmpty: true, reason: "theo xác nhận của người dùng");
        }
        catch (Exception ex)
        {
            _log.Error("Conflict", "Xóa mod xung đột thất bại: " + ex.Message, ex);
            return Result<int>.Fail("Xóa mod xung đột thất bại: " + ex.Message, ex);
        }
    }

    /// <summary>
    /// Xóa đúng tập file đã được bộ dò conflict nhận diện. Caller phải giữ operation lock.
    /// Không đụng các pakchunk gốc của game vì CollectConflictFiles đã loại chúng.
    /// </summary>
    private Result<int> DeleteConflictsUnderLock(
        string gamePath,
        bool failWhenEmpty,
        string reason)
    {
        var targets = CollectConflictFiles(gamePath);
        if (targets.Count == 0)
            return failWhenEmpty
                ? Result<int>.Fail("Không tìm thấy file mod xung đột để xóa.")
                : Result<int>.Ok(0);

        var gameRoot = Path.GetFullPath(gamePath);

        // Hai pha: kiểm tra toàn bộ target trước khi xóa file đầu tiên. Nếu một file
        // đang bị process khác khóa hoặc thư mục cha không ghi/xóa được thì dừng nguyên vẹn.
        var preflightFailures = new List<string>();
        var checkedParents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            var relative = Path.GetRelativePath(gameRoot, target);
            if (relative.StartsWith("..", StringComparison.Ordinal))
            {
                preflightFailures.Add(relative + " (nằm ngoài thư mục game)");
                continue;
            }

            try
            {
                if (!File.Exists(target)) continue;
                var attributes = File.GetAttributes(target);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    preflightFailures.Add(relative + " (reparse/symlink; từ chối dọn tự động)");
                    continue;
                }

                using (new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.None)) { }

                var parent = Path.GetDirectoryName(target);
                if (!string.IsNullOrWhiteSpace(parent) && checkedParents.Add(parent))
                    EnsureDirectoryWritable(parent, "thư mục chứa mod xung đột");
            }
            catch (Exception ex)
            {
                preflightFailures.Add(relative + " (" + ex.Message + ")");
            }
        }

        if (preflightFailures.Count > 0)
            return Result<int>.Fail("Chưa xóa file nào vì preflight dọn mod thất bại:\n- "
                + string.Join("\n- ", preflightFailures.Take(8)));

        // Preserve the previous version.dll before removing an unverified proxy.
        // It stays inactive and is not automatically restored on uninstall unless
        // the current official game manifest explicitly verifies version.dll.
        var win64 = Win64Of(gamePath);
        var installedVersion = Path.Combine(win64, "version.dll");
        var versionBackup = Path.Combine(win64, "version_goc.dll");
        if (targets.Contains(installedVersion, StringComparer.OrdinalIgnoreCase)
            && File.Exists(installedVersion) && !File.Exists(versionBackup))
        {
            try
            {
                File.Copy(installedVersion, versionBackup, overwrite: false);
                if (!SameFile(installedVersion, versionBackup))
                {
                    File.Delete(versionBackup);
                    return Result<int>.Fail("Sao lưu version.dll không đạt SHA-256; chưa xóa mod nào.");
                }
            }
            catch (Exception ex)
            {
                return Result<int>.Fail("Không thể sao lưu version.dll trước khi xóa mod; chưa xóa file nào: " + ex.Message);
            }
        }

        var deleted = 0;
        var failures = new List<string>();
        foreach (var target in targets)
        {
            var relative = Path.GetRelativePath(gameRoot, target);
            if (relative.StartsWith("..", StringComparison.Ordinal))
            {
                failures.Add(relative + " (nằm ngoài thư mục game)");
                continue;
            }

            try
            {
                if (!File.Exists(target)) continue;
                var attributes = File.GetAttributes(target);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(target, attributes & ~FileAttributes.ReadOnly);
                File.Delete(target);
                if (File.Exists(target))
                    failures.Add(relative + " (Windows không cho xóa)");
                else
                    deleted++;
            }
            catch (Exception ex)
            {
                failures.Add(relative + " (" + ex.Message + ")");
            }
        }

        RemoveEmptyModDirectories(gamePath);
        if (failures.Count > 0)
        {
            _log.Warn("Conflict", $"Đã xóa {deleted} file; còn {failures.Count} file không xóa được.");
            return Result<int>.Fail($"Đã xóa {deleted} file nhưng còn {failures.Count} file không xóa được:\n- "
                + string.Join("\n- ", failures.Take(8)));
        }

        _log.Info("Conflict", $"Đã xóa vĩnh viễn {deleted} file mod xung đột {reason}.");
        return Result<int>.Ok(deleted);
    }

    private IReadOnlyList<string> CollectConflictFiles(string gamePath)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void AddFile(string path)
        {
            if (File.Exists(path) && !IsProtectedBackupFile(path)
                && !IsOfficialGameFile(gamePath, path))
                files.Add(Path.GetFullPath(path));
        }
        void AddTree(string path)
        {
            if (!Directory.Exists(path)) return;
            foreach (var file in SafeEnumerateFiles(path)) AddFile(file);
        }

        var paks = PaksOf(gamePath);
        var mods = ModsOf(gamePath);
        EnsureManagedMarkerReadable(mods);
        var win64 = Win64Of(gamePath);
        var markerExists = File.Exists(Path.Combine(mods, MarkerName));
        var marker = ViethoaInstallMarker.Load(Path.Combine(mods, MarkerName));
        var legacyBundle = IsLegacyVhwuwaBundle(gamePath, marker);
        var owned = OwnedModFiles(mods);

        if (Directory.Exists(paks))
        {
            foreach (var file in Directory.EnumerateFiles(paks, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(file);
                if (name.StartsWith("pakchunk", StringComparison.OrdinalIgnoreCase)
                    || !ModExtensions.Contains(Path.GetExtension(file))
                    || !Path.GetFileNameWithoutExtension(name).EndsWith("_P", StringComparison.OrdinalIgnoreCase))
                    continue;
                AddFile(file);
            }
            foreach (var dir in EnumerateModFolders(paks))
            {
                if (IsLinkedDirectory(dir)) continue;
                var isOwnDirectory = Path.GetFullPath(dir)
                    .Equals(Path.GetFullPath(mods), StringComparison.OrdinalIgnoreCase);
                foreach (var file in SafeEnumerateFiles(dir))
                {
                    var isTopLevel = Path.GetDirectoryName(file)!
                        .Equals(dir, StringComparison.OrdinalIgnoreCase);
                    var name = Path.GetFileName(file);
                    if (isOwnDirectory && isTopLevel && marker is null && IsReservedWahuModName(name)) continue;
                    if (isOwnDirectory && isTopLevel && IsLegacyVhwuwaModFile(file, legacyBundle)) continue;
                    if (isOwnDirectory && isTopLevel && markerExists && owned.Contains(name)
                        && MatchesLegacyManagedFile(marker, "mods", file)) continue;
                    AddFile(file);
                }
            }
        }

        foreach (var name in ProxyLoaders)
        {
            var installed = Path.Combine(win64, name);
            if (!File.Exists(installed)) continue;
            if (legacyBundle && name.Equals("version.dll", StringComparison.OrdinalIgnoreCase)) continue;
            if (markerExists && File.Exists(Path.Combine(LoaderDir, name))
                && MatchesLegacyManagedFile(marker, "win64", installed)) continue;
            AddFile(installed);
        }
        // A third-party mod may overwrite our loader without changing its filename.
        // Only quarantine it if the installed SHA differs from the managed marker.
        foreach (var name in new[] { "verorg.dll", "WuWaVH.dll" })
        {
            var installed = Path.Combine(win64, name);
            if (markerExists && File.Exists(installed)
                && !MatchesLegacyManagedFile(marker, "win64", installed))
                AddFile(installed);
        }
        foreach (var name in KnownForeignWin64Files)
            AddFile(Path.Combine(win64, name));
        // Backup files are never candidates for automatic cleanup.
        foreach (var dirName in KnownForeignWin64Dirs)
            AddTree(Path.Combine(win64, dirName));

        return files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void RemoveEmptyModDirectories(string gamePath)
    {
        var paks = PaksOf(gamePath);
        if (Directory.Exists(paks))
            foreach (var dir in EnumerateModFolders(paks))
                RemoveEmptyTree(dir);
        var win64 = Win64Of(gamePath);
        foreach (var name in KnownForeignWin64Dirs)
            RemoveEmptyTree(Path.Combine(win64, name));
    }

    private static void RemoveEmptyTree(string root)
    {
        try
        {
            if (!Directory.Exists(root)) return;
            if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) return;
            foreach (var dir in SafeEnumerateDirectories(root).OrderByDescending(x => x.Length))
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        Directory.Delete(dir);
                }
                catch { /* thư mục rỗng còn lại không được làm fail install */ }
            }

            try
            {
                if (!Directory.EnumerateFileSystemEntries(root).Any())
                    Directory.Delete(root);
            }
            catch { /* best-effort */ }
        }
        catch { /* cleanup thư mục rỗng không phải lỗi cài đặt */ }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root)
    {
        if (IsLinkedDirectory(root)) yield break;
        foreach (var directory in new[] { root }.Concat(SafeEnumerateDirectories(root)))
        {
            string[] files;
            try { files = Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly); }
            catch { continue; }
            foreach (var file in files) yield return file;
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            string[] children;
            try { children = Directory.GetDirectories(current, "*", SearchOption.TopDirectoryOnly); }
            catch { continue; }
            foreach (var child in children)
            {
                FileAttributes attributes;
                try { attributes = File.GetAttributes(child); }
                catch { continue; }
                if ((attributes & FileAttributes.ReparsePoint) != 0) continue;
                pending.Push(child);
                yield return child;
            }
        }
    }

    /// <summary>Tìm 1 file .sig gốc trong Paks\ để làm "hạt giống" cho .sig của mod (loader chỉ cần .sig TỒN TẠI).</summary>
    private static string? FindSeedSig(string paksDir)
    {
        var prefer = Path.Combine(paksDir, "pakchunk0optional-WindowsNoEditor.sig");
        if (File.Exists(prefer)) return prefer;
        try
        {
            return SafeEnumerateFiles(paksDir)
                .FirstOrDefault(file => Path.GetExtension(file).Equals(".sig", StringComparison.OrdinalIgnoreCase));
        }
        catch { return null; }
    }

    private static Exception ManagedWriteException(string action, string path, Exception inner)
    {
        if (inner is UnauthorizedAccessException)
        {
            return new UnauthorizedAccessException(
                $"Windows từ chối quyền khi {action} '{path}'.\n" +
                "👉 Hãy đóng VHWuWa, nhấp chuột phải VHWuWa.exe và chọn 'Run as administrator' (Chạy với tư cách quản trị viên), rồi thử lại. " +
                "Nếu game nằm trong Program Files, hãy bảo đảm tài khoản Windows có quyền ghi vào thư mục game.",
                inner);
        }

        return new IOException(
            $"Không thể {action} '{path}'. File có thể đang bị game, launcher hoặc chương trình khác sử dụng.\n" +
            "👉 Hãy đóng hoàn toàn Wuthering Waves/launcher rồi thử lại. Nếu vẫn lỗi, hãy chạy VHWuWa bằng quyền Administrator.",
            inner);
    }

    private static void EnsureManagedTargetAvailable(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("File đích là symbolic link/reparse point: " + path);
            // Verify locks/permissions before deleting any old mod. Do not modify bytes here.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw ManagedWriteException("kiểm tra file đích trước khi dọn mod", path, ex);
        }
    }

    private static void EnsureDirectoryWritable(string directory, string label)
    {
        string? probe = null;
        try
        {
            Directory.CreateDirectory(directory);
            probe = Path.Combine(directory, ".vhwuwa-write-test-" + Guid.NewGuid().ToString("N") + ".tmp");
            using (var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                stream.WriteByte(0);
            File.Delete(probe);
            probe = null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw ManagedWriteException($"ghi vào {label}", directory, ex);
        }
        finally
        {
            if (probe is not null)
            {
                try { if (File.Exists(probe)) File.Delete(probe); } catch { }
            }
        }
    }

    private static void PrepareManagedDestination(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw ManagedWriteException("chuẩn bị ghi đè file", path, ex);
        }
    }

    private static void CopyManagedFileOverwrite(string source, string destination)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            PrepareManagedDestination(destination);
            File.Copy(source, destination, overwrite: true);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw ManagedWriteException("ghi đè file", destination, ex);
        }
    }

    private static void MoveManagedFileOverwrite(string source, string destination)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            PrepareManagedDestination(destination);
            File.Move(source, destination, overwrite: true);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw ManagedWriteException("ghi đè file", destination, ex);
        }
    }

    private static void DeleteManagedFile(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            PrepareManagedDestination(path);
            File.Delete(path);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw ManagedWriteException("xóa file cũ", path, ex);
        }
    }

    private static void WriteSig(string? seed, string dstSig)
    {
        if (seed is not null && File.Exists(seed))
        {
            CopyManagedFileOverwrite(seed, dstSig);
            return;
        }

        try
        {
            PrepareManagedDestination(dstSig);
            File.WriteAllBytes(dstSig, Array.Empty<byte>());   // placeholder rỗng
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            throw ManagedWriteException("ghi đè chữ ký file", dstSig, ex);
        }
    }

    public async Task<Result> UpdateTranslationPakAsync(string gamePath, NameVariant variant,
        CancellationToken ct = default)
    {
        LastAutoDeletedConflictCount = 0;
        try
        {
            var status = GetStatus(gamePath);
            if (!status.Installed)
                return Result.Fail("Chưa phát hiện bản Việt hóa do VHWuWa cài trong thư mục game này.");
            if (IsGameRunning())
                return Result.Fail("Game hoặc launcher Wuthering Waves đang chạy. Hãy đóng hẳn game/launcher rồi mới cập nhật.");

            using var operationLock = TryAcquireOperationLock(gamePath);
            if (operationLock is null)
                return Result.Fail("Đang có một tiến trình VHWuWa khác cài/gỡ/cập nhật. Hãy chờ tiến trình đó hoàn tất.");
            // The install/update path removes external loaders under the same operation
            // lock, after validating package, permissions and managed destinations.

            var srcPak = variant == NameVariant.English ? EnPak : HanVietPak;
            if (!File.Exists(srcPak))
                return Result.Fail($"Không tìm thấy pak biến thể đã chọn: {Path.GetFileName(srcPak)}");

            var mods = ModsOf(gamePath);
            var markerPath = Path.Combine(mods, MarkerName);
            var marker = ViethoaInstallMarker.Load(markerPath);
            var legacyBundle = IsLegacyVhwuwaBundle(gamePath, marker);
            if (marker is null && !legacyBundle)
                return Result.Fail("Thiếu thông tin quản lý bản Việt hóa; không cập nhật nhanh để tránh ghi đè file không xác định.");

            marker ??= new ViethoaInstallMarker
            {
                PackageVersion = "",
                Variant = variant == NameVariant.English ? "en" : "hanviet",
                Font = File.Exists(Path.Combine(mods, "WahuFont_100_P.pak")) ? "WahuFont_100_P.pak" : null
            };

            var pakDst = Path.Combine(mods, PakName);
            var paks = PaksOf(gamePath);
            var win64 = Win64Of(gamePath);

            // Preflight trước khi purge: nếu package/quyền ghi chưa sẵn sàng thì
            // tuyệt đối không đụng vào mod/runtime cũ.
            EnsureDirectoryWritable(paks, "thư mục Paks của game");
            EnsureDirectoryWritable(mods, "thư mục mod của VHWuWa");
            EnsureDirectoryWritable(win64, "thư mục Win64 của game");
            EnsureManagedTargetAvailable(pakDst);
            EnsureManagedTargetAvailable(Path.ChangeExtension(pakDst, ".sig"));
            EnsureManagedTargetAvailable(markerPath);
            foreach (var dll in new[] { "version.dll", "verorg.dll", "WuWaVH.dll" })
                EnsureManagedTargetAvailable(Path.Combine(win64, dll));
            foreach (var dll in new[] { "version.dll", "verorg.dll", "WuWaVH.dll" })
            {
                var source = Path.Combine(LoaderDir, dll);
                if (!File.Exists(source))
                    return Result.Fail("Thiếu loader canonical để cập nhật nhanh: " + source);
            }

            string? preflightFontSource = null;
            string? preflightFontName = null;
            if (!string.IsNullOrWhiteSpace(marker.Font))
            {
                var requestedFont = Path.Combine(_contentDir, "font", Path.GetFileName(marker.Font));
                preflightFontSource = File.Exists(requestedFont) ? requestedFont : FindFontPak();
                if (preflightFontSource is null)
                    return Result.Fail("Thiếu font đã cài để tự phục hồi; chưa dọn mod/file cũ.");
                preflightFontName = Path.GetFileName(preflightFontSource);
            }

            // Only accept cancellation before modifying game files.
            ct.ThrowIfCancellationRequested();
            RemoveEmptyModDirectories(gamePath);

            // Cập nhật nhanh có quyền tự chữa bản cài. Nếu mod khác đã
            // ghi đè PAK/loader hoặc chèn bundle xung đột, dọn đúng tập file
            // detector nhận diện rồi ghi lại PAK canonical của release.
            var conflicts = FindConflicts(gamePath);
            var scanError = conflicts.FirstOrDefault(x =>
                x.StartsWith("Không thể kiểm tra mod:", StringComparison.OrdinalIgnoreCase));
            if (scanError is not null)
                return Result.Fail(scanError);
            if (conflicts.Count > 0)
            {
                var cleanup = QuarantineConflictsCore(gamePath, acquireLock: false);
                if (!cleanup.Success || cleanup.Value is null)
                    return Result.Fail("Không thể xử lý mod xung đột trước cập nhật nhanh: " + cleanup.Error);
                LastAutoDeletedConflictCount = cleanup.Value.MovedFiles.Count;

                var remaining = FindConflicts(gamePath);
                var remainingScanError = remaining.FirstOrDefault(x =>
                    x.StartsWith("Không thể kiểm tra mod:", StringComparison.OrdinalIgnoreCase));
                if (remainingScanError is not null)
                    return Result.Fail(remainingScanError);
                if (remaining.Count > 0)
                    return Result.Fail("Dọn mod ngoài chưa sạch; chưa cập nhật PAK mới:\n- "
                        + string.Join("\n- ", remaining.Take(8)));
            }

            // A known foreign loader must not survive into the new installation.
            if (HasForeignWinHttpPakLoader(gamePath))
                return Result.Fail("Còn loader/mod không thể cách ly an toàn; chưa cập nhật.");
            Directory.CreateDirectory(mods);

            string? repairedFontSource = preflightFontSource;
            string? repairedFontName = preflightFontName;
            await Task.Run(() =>
            {
                var seedSig = FindSeedSig(paks);

                var temp = pakDst + ".update-" + Guid.NewGuid().ToString("N");
                try
                {
                    File.Copy(srcPak, temp, true);
                    if (!string.Equals(ViethoaInstallMarker.Sha256(srcPak), ViethoaInstallMarker.Sha256(temp),
                            StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("SHA-256 của PAK tạm không khớp nguồn.");
                    MoveManagedFileOverwrite(temp, pakDst);
                }
                finally
                {
                    try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                }

                if (!string.Equals(ViethoaInstallMarker.Sha256(srcPak), ViethoaInstallMarker.Sha256(pakDst),
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Hậu kiểm SHA-256 PAK đang hoạt động thất bại.");

                // Self-heal chữ ký PAK nếu mod cũ đã xóa/ghi đè.
                var pakSig = Path.ChangeExtension(pakDst, ".sig");
                WriteSig(seedSig, pakSig);

                // Self-heal font đang được marker quản lý. Nếu package hiện tại chỉ
                // còn một font canonical khác tên, dùng font đó và cập nhật marker.
                if (!string.IsNullOrWhiteSpace(marker.Font))
                {
                    var fontSource = repairedFontSource
                        ?? throw new InvalidOperationException("Preflight font không hợp lệ.");
                    repairedFontName ??= Path.GetFileName(fontSource);
                    var fontDst = Path.Combine(mods, repairedFontName);
                    CopyManagedFileOverwrite(fontSource, fontDst);
                    WriteSig(seedSig, Path.ChangeExtension(fontDst, ".sig"));
                    marker.Font = repairedFontName;
                    marker.Track("mods", fontDst);
                    marker.Track("mods", Path.ChangeExtension(fontDst, ".sig"));
                }

                // Self-heal loader: quick update không được để thiếu loader sau khi
                // đã dọn proxy/version.dll của mod khác.
                foreach (var dll in new[] { "version.dll", "verorg.dll", "WuWaVH.dll" })
                {
                    var source = Path.Combine(LoaderDir, dll);
                    if (!File.Exists(source))
                        throw new FileNotFoundException("Thiếu loader canonical để tự phục hồi.", source);
                    var destination = Path.Combine(win64, dll);
                    CopyManagedFileOverwrite(source, destination);
                    marker.Track("win64", destination);
                }

                marker.PackageVersion = CurrentPackageVersion();
                marker.Variant = variant == NameVariant.English ? "en" : "hanviet";
                marker.Track("mods", pakDst);
                marker.Track("mods", pakSig);
                PrepareManagedDestination(markerPath);
                marker.Save(markerPath);
            }, CancellationToken.None);

            var verifyErrors = VerifyInstallation(
                gamePath, srcPak, variant, repairedFontSource, repairedFontName);
            if (verifyErrors.Count > 0)
                return Result.Fail("Cập nhật nhanh đã ghi file nhưng hậu kiểm self-heal thất bại:\n- "
                    + string.Join("\n- ", verifyErrors));

            _log.Info("Viethoa", $"Đã cập nhật nhanh {PakName} ({variant}), tự chữa loader/font/.sig và đồng bộ marker SHA.");
            return Result.Ok();
        }
        catch (OperationCanceledException) { return Result.Fail("Đã hủy cập nhật PAK."); }
        catch (UnauthorizedAccessException ex)
        {
            _log.Error("Viethoa", "Cập nhật PAK bị Windows từ chối quyền: " + ex.Message, ex);
            return Result.Fail(ex.Message, ex);
        }
        catch (IOException ex)
        {
            _log.Error("Viethoa", "Cập nhật PAK không thể ghi đè file: " + ex.Message, ex);
            return Result.Fail(ex.Message, ex);
        }
        catch (Exception ex)
        {
            _log.Error("Viethoa", "Cập nhật PAK thất bại: " + ex.Message, ex);
            return Result.Fail("Cập nhật PAK thất bại: " + ex.Message, ex);
        }
    }

    public async Task<Result> InstallAsync(string gamePath, NameVariant variant, bool withFont,
        IProgress<InstallProgress>? progress = null, CancellationToken ct = default)
    {
        LastAutoDeletedConflictCount = 0;
        try
        {
            var win64 = Win64Of(gamePath);
            if (!File.Exists(Path.Combine(win64, "Client-Win64-Shipping.exe")))
                return Result.Fail("Không thấy Client-Win64-Shipping.exe — hãy chọn đúng thư mục game (chứa Client\\).");

            var paks = PaksOf(gamePath);
            if (!Directory.Exists(paks))
                return Result.Fail("Không thấy Client\\Content\\Paks — thư mục game không hợp lệ.");

            if (IsGameRunning())
                return Result.Fail("Game hoặc launcher Wuthering Waves đang chạy. Hãy đóng hẳn game/launcher rồi mới cài.");

            using var operationLock = TryAcquireOperationLock(gamePath);
            if (operationLock is null)
                return Result.Fail("Đang có một tiến trình VHWuWa khác cài/gỡ. Hãy chờ tiến trình đó hoàn tất.");

            // The install/update path removes external loaders under the same operation
            // lock, after validating package, permissions and managed destinations.

            var content = InspectContent();
            if (!content.Ready)
                return Result.Fail("Thiếu nội dung Việt hóa (thư mục content\\ chưa đủ pak/loader).");

            var srcPak = variant == NameVariant.English ? EnPak : HanVietPak;
            if (!File.Exists(srcPak))
                return Result.Fail($"Không tìm thấy pak biến thể đã chọn: {Path.GetFileName(srcPak)}");
            if (withFont && content.FontPak is null)
                return Result.Fail("Đã chọn cài font nhưng package hiện tại không có PAK font.");

            var mods = ModsOf(gamePath);
            // Preflight quyền ghi trước khi purge bất kỳ mod/file cũ nào.
            EnsureDirectoryWritable(paks, "thư mục Paks của game");
            EnsureDirectoryWritable(mods, "thư mục mod của VHWuWa");
            EnsureDirectoryWritable(win64, "thư mục Win64 của game");
            EnsureManagedTargetAvailable(Path.Combine(mods, PakName));
            EnsureManagedTargetAvailable(Path.Combine(mods, Path.ChangeExtension(PakName, ".sig")));
            EnsureManagedTargetAvailable(Path.Combine(mods, MarkerName));
            foreach (var loader in new[] { "version.dll", "verorg.dll", "WuWaVH.dll" })
                EnsureManagedTargetAvailable(Path.Combine(win64, loader));
            if (withFont && content.FontPak is not null)
            {
                var selectedFont = Path.Combine(mods, Path.GetFileName(content.FontPak));
                EnsureManagedTargetAvailable(selectedFont);
                EnsureManagedTargetAvailable(Path.ChangeExtension(selectedFont, ".sig"));
            }

            // Only accept cancellation before modifying game files.
            ct.ThrowIfCancellationRequested();
            RemoveEmptyModDirectories(gamePath);

            var conflicts = FindConflicts(gamePath);
            var scanError = conflicts.FirstOrDefault(x =>
                x.StartsWith("Không thể kiểm tra mod:", StringComparison.OrdinalIgnoreCase));
            if (scanError is not null)
                return Result.Fail(scanError);
            if (conflicts.Count > 0)
            {
                var cleanup = QuarantineConflictsCore(gamePath, acquireLock: false);
                if (!cleanup.Success || cleanup.Value is null)
                    return Result.Fail("Không thể xử lý mod xung đột trước khi cài:\n" + cleanup.Error);
                LastAutoDeletedConflictCount = cleanup.Value.MovedFiles.Count;

                var remaining = FindConflicts(gamePath);
                var remainingScanError = remaining.FirstOrDefault(x =>
                    x.StartsWith("Không thể kiểm tra mod:", StringComparison.OrdinalIgnoreCase));
                if (remainingScanError is not null)
                    return Result.Fail(remainingScanError);
                if (remaining.Count > 0)
                    return Result.Fail("Dọn mod ngoài chưa sạch; chưa ghi bản Việt hóa mới:\n- "
                        + string.Join("\n- ", remaining.Take(8)));
            }

            // A known foreign loader must not survive into the new installation.
            if (HasForeignWinHttpPakLoader(gamePath))
                return Result.Fail("Còn loader/mod không thể cách ly an toàn; chưa cập nhật.");
            Directory.CreateDirectory(mods);

            var existingMarker = ViethoaInstallMarker.Load(Path.Combine(mods, MarkerName));
            var legacyBundle = IsLegacyVhwuwaBundle(gamePath, existingMarker);
            string? fontName = (withFont && content.FontPak is not null)
                ? Path.GetFileName(content.FontPak) : null;

            await Task.Run(() =>
            {
                // Quyền ghi đã được preflight trước purge. Từ đây operation phải chạy
                // đến hậu kiểm để tránh trạng thái nửa cài nếu người dùng bấm Hủy.
                // Giữ các file đích hiện tại để ghi đè trực tiếp. Chỉ xóa file managed
                // cũ không còn thuộc cấu hình mới (ví dụ font cũ khi đổi/bỏ font).
                var owned = OwnedModFiles(mods);
                var keepOwned = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    PakName,
                    Path.ChangeExtension(PakName, ".sig"),
                    MarkerName
                };
                if (fontName is not null)
                {
                    keepOwned.Add(fontName);
                    keepOwned.Add(Path.ChangeExtension(fontName, ".sig"));
                }

                foreach (var old in Directory.EnumerateFiles(mods, "*", SearchOption.TopDirectoryOnly))
                {
                    var name = Path.GetFileName(old);
                    if (owned.Contains(name) && !keepOwned.Contains(name))
                        DeleteManagedFile(old);
                }

                var seedSig = FindSeedSig(paks);

                // 1) PAK bản dịch + .sig: luôn ghi đè bản VHWuWa cũ.
                var pakDst = Path.Combine(mods, PakName);
                CopyManagedFileOverwrite(srcPak, pakDst);
                WriteSig(seedSig, Path.ChangeExtension(pakDst, ".sig"));
                progress?.Report(new InstallProgress(40, PakName, 1, fontName is null ? 2 : 3));

                // 2) Font pak + .sig: luôn ghi đè bản VHWuWa cũ.
                if (fontName is not null)
                {
                    var fontDst = Path.Combine(mods, fontName);
                    CopyManagedFileOverwrite(content.FontPak!, fontDst);
                    WriteSig(seedSig, Path.ChangeExtension(fontDst, ".sig"));
                    progress?.Report(new InstallProgress(70, fontName, 2, 3));
                }

                // 3) Loader vào Win64 (backup version.dll gốc 1 lần).
                var verOrig = Path.Combine(win64, "version.dll");
                var verBak = Path.Combine(win64, "version_goc.dll");
                var versionAlreadyManaged = existingMarker is not null
                    && File.Exists(verOrig)
                    && MatchesLegacyManagedFile(existingMarker, "win64", verOrig);
                if (File.Exists(verOrig) && !File.Exists(verBak) && !legacyBundle && !versionAlreadyManaged)
                {
                    try { File.Copy(verOrig, verBak, false); }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                    {
                        throw ManagedWriteException("tạo bản sao loader gốc", verBak, ex);
                    }
                }

                foreach (var dll in new[] { "version.dll", "verorg.dll", "WuWaVH.dll" })
                {
                    var source = Path.Combine(LoaderDir, dll);
                    var destination = Path.Combine(win64, dll);
                    CopyManagedFileOverwrite(source, destination);
                }
                progress?.Report(new InstallProgress(100, "loader", fontName is null ? 2 : 3, fontName is null ? 2 : 3));

                // Marker v2 lưu SHA-256 của từng file do app quản lý. Khi mod khác
                // ghi đè đúng tên file, app sẽ cảnh báo và không xóa nhầm lúc gỡ.
                var marker = new ViethoaInstallMarker
                {
                    PackageVersion = CurrentPackageVersion(),
                    Variant = variant == NameVariant.English ? "en" : "hanviet",
                    Font = fontName
                };
                marker.Track("mods", pakDst);
                marker.Track("mods", Path.ChangeExtension(pakDst, ".sig"));
                if (fontName is not null)
                {
                    marker.Track("mods", Path.Combine(mods, fontName));
                    marker.Track("mods", Path.Combine(mods, Path.ChangeExtension(fontName, ".sig")));
                }
                foreach (var dll in new[] { "version.dll", "verorg.dll", "WuWaVH.dll" })
                    marker.Track("win64", Path.Combine(win64, dll));

                var markerPath = Path.Combine(mods, MarkerName);
                PrepareManagedDestination(markerPath);
                marker.Save(markerPath);
            }, CancellationToken.None);

            var verifyErrors = VerifyInstallation(
                gamePath, srcPak, variant, fontName is null ? null : content.FontPak, fontName);
            if (verifyErrors.Count > 0)
            {
                var detail = string.Join("\n- ", verifyErrors);
                _log.Error("Viethoa", "Hậu kiểm cài đặt thất bại:\n- " + detail);
                return Result.Fail("Cài chưa đầy đủ, hậu kiểm file thất bại:\n- " + detail
                    + "\nĐừng mở game; hãy bấm Gỡ Việt hóa rồi cài lại.");
            }
            progress?.Report(new InstallProgress(100, "Hậu kiểm dung lượng + SHA-256: đạt",
                fontName is null ? 6 : 8, fontName is null ? 6 : 8));

            _log.Info("Viethoa", $"Đã cài và hậu kiểm Việt hóa ({variant}) vào {gamePath}");
            return Result.Ok();
        }
        catch (OperationCanceledException) { return Result.Fail("Đã hủy cài đặt."); }
        catch (UnauthorizedAccessException ex)
        {
            _log.Error("Viethoa", "Cài đặt bị Windows từ chối quyền: " + ex.Message, ex);
            return Result.Fail(ex.Message, ex);
        }
        catch (IOException ex)
        {
            _log.Error("Viethoa", "Cài đặt không thể ghi đè file: " + ex.Message, ex);
            return Result.Fail(ex.Message, ex);
        }
        catch (Exception ex)
        {
            _log.Error("Viethoa", "Cài đặt thất bại: " + ex.Message, ex);
            return Result.Fail("Cài đặt thất bại: " + ex.Message, ex);
        }
    }

    public async Task<Result> UninstallAsync(string gamePath, bool forceRemoveChangedFiles = false,
        CancellationToken ct = default)
    {
        try
        {
            var win64 = Win64Of(gamePath);
            if (!GetStatus(gamePath).Installed)
                return Result.Fail("Chưa phát hiện bản Việt hóa do VHWuWa cài trong thư mục game này.");
            if (IsGameRunning())
                return Result.Fail("Game hoặc launcher Wuthering Waves đang chạy. Hãy đóng hẳn game/launcher rồi mới gỡ.");

            using var operationLock = TryAcquireOperationLock(gamePath);
            if (operationLock is null)
                return Result.Fail("Đang có một tiến trình VHWuWa khác cài/gỡ. Hãy chờ tiến trình đó hoàn tất.");

            var modsPath = ModsOf(gamePath);
            EnsureManagedMarkerReadable(modsPath);
            var verifiedMarker = ViethoaInstallMarker.Load(Path.Combine(modsPath, MarkerName));
            if (verifiedMarker is null && !IsLegacyVhwuwaBundle(gamePath, null))
                return Result.Fail("Không đủ bằng chứng đây là bản Việt hóa WAHU: thiếu marker và bộ loader/PAK legacy đầy đủ. Dừng gỡ để bảo vệ file không rõ nguồn gốc.");

            var changedFiles = FindManagedFileChanges(gamePath);
            if (changedFiles.Count > 0 && !forceRemoveChangedFiles)
                return Result.Fail("Không gỡ để tránh xóa nhầm file của mod khác. Các file do VHWuWa quản lý đã bị thay đổi:\n- "
                    + string.Join("\n- ", changedFiles)
                    + "\nBạn có thể xác nhận Xóa luôn trên giao diện nếu vẫn muốn gỡ.");
            if (changedFiles.Count > 0)
                _log.Warn("Viethoa", $"Người dùng xác nhận gỡ cưỡng bức {changedFiles.Count} file đã bị thay đổi.");

            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var mods = ModsOf(gamePath);
                var owned = OwnedModFiles(mods);

                // Chỉ gỡ loader khi hậu kiểm đã xác nhận chúng vẫn là file của WAHU.
                foreach (var dll in new[] { "WuWaVH.dll", "verorg.dll" })
                {
                    var p = Path.Combine(win64, dll);
                    if (File.Exists(p)) File.Delete(p);
                }

                var ver = Path.Combine(win64, "version.dll");
                var verBak = Path.Combine(win64, "version_goc.dll");
                var officialVersionDll = LatestOfficialManifestContains(
                    gamePath, "Client/Binaries/Win64/version.dll");
                if (File.Exists(verBak))
                {
                    if (officialVersionDll == true)
                    {
                        // Chỉ phục hồi khi manifest hiện tại xác nhận rõ version.dll là file official.
                        CopyManagedFileOverwrite(verBak, ver);
                        DeleteManagedFile(verBak);
                    }
                    else
                    {
                        // Nếu không xác minh được version.dll là file chính thức,
                        // không khôi phục proxy cũ vào game. Giữ bản sao lưu để
                        // người dùng có thể tự kiểm tra hoặc khôi phục khi cần.
                        DeleteManagedFile(ver);
                    }
                }
                else
                {
                    DeleteManagedFile(ver);
                }

                // Log này do loader VHWuWa sinh ra, không phải file game.
                DeleteManagedFile(Path.Combine(win64, "wahu_loader_log.txt"));

                // Giữ nguyên mọi file không được marker sở hữu, kể cả mod *_100_P.pak.
                if (Directory.Exists(mods))
                {
                    foreach (var file in Directory.EnumerateFiles(mods, "*", SearchOption.TopDirectoryOnly))
                    {
                        var name = Path.GetFileName(file);
                        if (owned.Contains(name) && !name.Equals(MarkerName, StringComparison.OrdinalIgnoreCase))
                            File.Delete(file);
                    }
                    var markerPath = Path.Combine(mods, MarkerName);
                    if (File.Exists(markerPath)) File.Delete(markerPath);
                    if (!Directory.EnumerateFileSystemEntries(mods).Any()) Directory.Delete(mods);
                }
            }, ct);
            _log.Info("Viethoa", "Đã gỡ Việt hóa: " + gamePath);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _log.Error("Viethoa", "Gỡ cài đặt thất bại: " + ex.Message, ex);
            return Result.Fail("Gỡ cài đặt thất bại: " + ex.Message, ex);
        }
    }
}
