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
        { "version.dll", "dxgi.dll", "dinput8.dll", "winmm.dll", "xinput1_3.dll", "xinput1_4.dll", "dsound.dll", "winhttp.dll" };
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
    private static string PaksOf(string gamePath) =>
        Path.Combine(gamePath, "Client", "Content", "Paks");
    private static string ModsOf(string gamePath) =>
        Path.Combine(PaksOf(gamePath), ModsFolder);

    private static bool DetectGameRunning()
    {
        try
        {
            var processes = Process.GetProcessesByName("Client-Win64-Shipping")
                .Concat(Process.GetProcessesByName("Wuthering Waves"))
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
            st.Installed = File.Exists(Path.Combine(mods, PakName))
                        || File.Exists(Path.Combine(win64, "WuWaVH.dll"))
                        || File.Exists(Path.Combine(win64, "verorg.dll"))
                        || (File.Exists(Path.Combine(win64, "version.dll")) && File.Exists(Path.Combine(win64, "version_goc.dll")));
            var marker = ViethoaInstallMarker.Load(Path.Combine(mods, MarkerName));
            if (marker is not null)
            {
                st.Version = string.IsNullOrWhiteSpace(marker.PackageVersion)
                    ? CurrentPackageVersion()
                    : marker.PackageVersion;
                st.Variant = marker.Variant;
                st.FontPak = marker.Font;
            }
            else if (st.Installed) st.Version = CurrentPackageVersion();
        }
        catch (Exception ex) { _log.Warn("Viethoa", "Đọc trạng thái lỗi: " + ex.Message); }
        return st;
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
        return errors;
    }

    /// <summary>Dò mod phổ biến nhưng tránh quét nhầm pakchunk chính thức ở gốc Paks.</summary>
    public IReadOnlyList<string> FindConflicts(string gamePath)
    {
        var conflicts = new List<string>();
        try
        {
            var paks = PaksOf(gamePath);
            var mods = ModsOf(gamePath);
            var markerExists = File.Exists(Path.Combine(mods, MarkerName));
            var marker = ViethoaInstallMarker.Load(Path.Combine(mods, MarkerName));
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
                        || name.StartsWith("pakchunk", StringComparison.OrdinalIgnoreCase)
                        || !Path.GetFileNameWithoutExtension(name).EndsWith("_P", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (extension.Equals(".sig", StringComparison.OrdinalIgnoreCase)
                        && File.Exists(Path.ChangeExtension(file, ".pak"))) continue;
                    conflicts.Add("Mod ngoài ở thư mục Paks: " + name);
                }

                foreach (var dir in Directory.EnumerateDirectories(paks, "~*", SearchOption.TopDirectoryOnly))
                {
                    if (Path.GetFullPath(dir).Equals(Path.GetFullPath(mods), StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                        {
                            if (!ModExtensions.Contains(Path.GetExtension(file))) continue;
                            var name = Path.GetFileName(file);
                            if (markerExists && owned.Contains(name))
                            {
                                if (!MatchesLegacyManagedFile(marker, "mods", file))
                                    conflicts.Add("File của Việt hóa đã bị mod khác ghi đè: " + Path.GetRelativePath(paks, file));
                                continue;
                            }
                            // Một .sig đi cùng .pak chỉ là file phụ; báo tên PAK một lần cho dễ hiểu.
                            if (Path.GetExtension(file).Equals(".sig", StringComparison.OrdinalIgnoreCase)
                                && File.Exists(Path.ChangeExtension(file, ".pak"))) continue;
                            conflicts.Add("Mod khác: " + Path.GetRelativePath(paks, file));
                        }
                        continue;
                    }
                    foreach (var file in SafeEnumerateFiles(dir))
                        if (ModExtensions.Contains(Path.GetExtension(file)))
                            conflicts.Add("Thư mục mod khác: " + Path.GetRelativePath(paks, file));
                }
            }

            var win64 = Win64Of(gamePath);
            foreach (var name in ProxyLoaders)
            {
                var installed = Path.Combine(win64, name);
                if (!File.Exists(installed)) continue;
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
            foreach (var dirName in new[] { "Mods", "ue4ss", "RE-UE4SS" })
                if (Directory.Exists(Path.Combine(win64, dirName)))
                    conflicts.Add("Bộ nạp mod khác: Win64\\" + dirName + "\\");
            if (Directory.Exists(Path.Combine(win64, "wuwaVietHoa")))
                conflicts.Add("Bộ Việt hóa khác: Win64\\wuwaVietHoa\\ (PAK/font được nạp qua proxy)");
        }
        catch (Exception ex)
        {
            conflicts.Add("Không thể kiểm tra mod: " + ex.Message);
        }
        return conflicts.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    }

    public Result<ModQuarantineReport> QuarantineConflicts(string gamePath)
    {
        var moved = new List<(string Source, string Target, string Relative, string Hash)>();
        try
        {
            if (IsGameRunning())
                return Result<ModQuarantineReport>.Fail("Game hoặc launcher đang chạy. Hãy đóng hẳn trước khi dọn mod xung đột.");

            using var operationLock = TryAcquireOperationLock(gamePath);
            if (operationLock is null)
                return Result<ModQuarantineReport>.Fail("Đang có một tiến trình VHWuWa khác cài/gỡ. Hãy chờ hoàn tất.");

            var targets = CollectConflictFiles(gamePath);
            if (targets.Count == 0)
                return Result<ModQuarantineReport>.Fail("Không tìm thấy file mod xung đột có thể cách ly an toàn.");

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

            var targets = CollectConflictFiles(gamePath);
            if (targets.Count == 0)
                return Result<int>.Fail("Không tìm thấy file mod xung đột để xóa.");

            var gameRoot = Path.GetFullPath(gamePath);
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

            _log.Info("Conflict", $"Đã xóa vĩnh viễn {deleted} file mod xung đột theo xác nhận của người dùng.");
            return Result<int>.Ok(deleted);
        }
        catch (Exception ex)
        {
            _log.Error("Conflict", "Xóa mod xung đột thất bại: " + ex.Message, ex);
            return Result<int>.Fail("Xóa mod xung đột thất bại: " + ex.Message, ex);
        }
    }

    private IReadOnlyList<string> CollectConflictFiles(string gamePath)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void AddFile(string path) { if (File.Exists(path)) files.Add(Path.GetFullPath(path)); }
        void AddTree(string path)
        {
            if (!Directory.Exists(path)) return;
            foreach (var file in SafeEnumerateFiles(path)) AddFile(file);
        }

        var paks = PaksOf(gamePath);
        var mods = ModsOf(gamePath);
        var win64 = Win64Of(gamePath);
        var markerExists = File.Exists(Path.Combine(mods, MarkerName));
        var marker = ViethoaInstallMarker.Load(Path.Combine(mods, MarkerName));
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
            foreach (var dir in Directory.EnumerateDirectories(paks, "~*", SearchOption.TopDirectoryOnly))
            {
                var isOwnDirectory = Path.GetFullPath(dir).Equals(Path.GetFullPath(mods), StringComparison.OrdinalIgnoreCase);
                foreach (var file in SafeEnumerateFiles(dir))
                {
                    if (!ModExtensions.Contains(Path.GetExtension(file))) continue;
                    var isTopLevel = Path.GetDirectoryName(file)!.Equals(dir, StringComparison.OrdinalIgnoreCase);
                    var name = Path.GetFileName(file);
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
            if (markerExists && File.Exists(Path.Combine(LoaderDir, name))
                && MatchesLegacyManagedFile(marker, "win64", installed)) continue;
            AddFile(installed);
        }
        foreach (var dirName in new[] { "Mods", "ue4ss", "RE-UE4SS", "wuwaVietHoa" })
            AddTree(Path.Combine(win64, dirName));

        return files.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void RemoveEmptyModDirectories(string gamePath)
    {
        var paks = PaksOf(gamePath);
        if (Directory.Exists(paks))
            foreach (var dir in Directory.EnumerateDirectories(paks, "~*", SearchOption.TopDirectoryOnly))
                RemoveEmptyTree(dir);
        var win64 = Win64Of(gamePath);
        foreach (var name in new[] { "Mods", "ue4ss", "RE-UE4SS", "wuwaVietHoa" })
            RemoveEmptyTree(Path.Combine(win64, name));
    }

    private static void RemoveEmptyTree(string root)
    {
        if (!Directory.Exists(root)) return;
        if ((File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) return;
        foreach (var dir in SafeEnumerateDirectories(root).OrderByDescending(x => x.Length))
            if (!Directory.EnumerateFileSystemEntries(dir).Any()) Directory.Delete(dir);
        if (!Directory.EnumerateFileSystemEntries(root).Any()) Directory.Delete(root);
    }

    private static IEnumerable<string> SafeEnumerateFiles(string root)
    {
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

    private static void WriteSig(string? seed, string dstSig)
    {
        if (seed is not null && File.Exists(seed)) File.Copy(seed, dstSig, true);
        else File.WriteAllBytes(dstSig, Array.Empty<byte>());   // placeholder rỗng
    }

    public async Task<Result> InstallAsync(string gamePath, NameVariant variant, bool withFont,
        IProgress<InstallProgress>? progress = null, CancellationToken ct = default)
    {
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

            var content = InspectContent();
            if (!content.Ready)
                return Result.Fail("Thiếu nội dung Việt hóa (thư mục content\\ chưa đủ pak/loader).");

            var conflicts = FindConflicts(gamePath);
            if (conflicts.Count > 0)
                return Result.Fail("Phát hiện mod có thể xung đột. Hãy gỡ/tắt mod đó trước:\n- "
                    + string.Join("\n- ", conflicts.Take(8)));

            var srcPak = variant == NameVariant.English ? EnPak : HanVietPak;
            if (!File.Exists(srcPak))
                return Result.Fail($"Không tìm thấy pak biến thể đã chọn: {Path.GetFileName(srcPak)}");

            var mods = ModsOf(gamePath);
            string? fontName = (withFont && content.FontPak is not null)
                ? Path.GetFileName(content.FontPak) : null;

            await Task.Run(() =>
            {
                Directory.CreateDirectory(mods);
                // Chỉ dọn file do chính WAHU tạo; không xóa nhầm mod của người dùng.
                var owned = OwnedModFiles(mods);
                foreach (var old in Directory.EnumerateFiles(mods, "*", SearchOption.TopDirectoryOnly))
                {
                    if (owned.Contains(Path.GetFileName(old)))
                        try { File.Delete(old); } catch { }
                }

                var seedSig = FindSeedSig(paks);

                // 1) Pak bản dịch + .sig
                var pakDst = Path.Combine(mods, PakName);
                File.Copy(srcPak, pakDst, true);
                WriteSig(seedSig, Path.ChangeExtension(pakDst, ".sig"));
                progress?.Report(new InstallProgress(40, PakName, 1, fontName is null ? 2 : 3));

                // 2) Font pak + .sig
                if (fontName is not null)
                {
                    var fontDst = Path.Combine(mods, fontName);
                    File.Copy(content.FontPak!, fontDst, true);
                    WriteSig(seedSig, Path.ChangeExtension(fontDst, ".sig"));
                    progress?.Report(new InstallProgress(70, fontName, 2, 3));
                }

                // 3) Loader vào Win64 (backup version.dll gốc 1 lần)
                var verOrig = Path.Combine(win64, "version.dll");
                var verBak = Path.Combine(win64, "version_goc.dll");
                if (File.Exists(verOrig) && !File.Exists(verBak))
                    File.Copy(verOrig, verBak, false);
                foreach (var dll in new[] { "version.dll", "verorg.dll", "WuWaVH.dll" })
                {
                    var s = Path.Combine(LoaderDir, dll);
                    File.Copy(s, Path.Combine(win64, dll), true);
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
                marker.Save(Path.Combine(mods, MarkerName));
            }, ct);

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
        catch (Exception ex)
        {
            _log.Error("Viethoa", "Cài lỗi: " + ex.Message, ex);
            return Result.Fail("Cài lỗi: " + ex.Message, ex);
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
                if (File.Exists(verBak))
                {
                    File.Copy(verBak, ver, true);
                    File.Delete(verBak);
                }
                else if (File.Exists(ver))
                {
                    File.Delete(ver);
                }

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
            _log.Error("Viethoa", "Gỡ lỗi: " + ex.Message, ex);
            return Result.Fail("Gỡ lỗi: " + ex.Message, ex);
        }
    }
}
