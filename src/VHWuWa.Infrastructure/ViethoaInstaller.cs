using System.Text.Json;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

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

    public ViethoaInstaller(ILogService log, string? contentDir = null)
    {
        _log = log;
        _contentDir = contentDir ?? Path.Combine(AppContext.BaseDirectory, "content");
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

    private static bool IsGameRunning()
    {
        try
        {
            return Process.GetProcessesByName("Client-Win64-Shipping").Length > 0
                || Process.GetProcessesByName("Wuthering Waves").Length > 0;
        }
        catch { return false; }
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
            var marker = Path.Combine(mods, MarkerName);
            if (File.Exists(marker))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(marker));
                if (doc.RootElement.TryGetProperty("variant", out var v)) st.Variant = v.GetString() ?? "";
                if (doc.RootElement.TryGetProperty("font", out var f)) st.FontPak = f.GetString();
            }
        }
        catch (Exception ex) { _log.Warn("Viethoa", "Đọc trạng thái lỗi: " + ex.Message); }
        return st;
    }

    private HashSet<string> OwnedModFiles(string mods)
    {
        var owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PakName, Path.ChangeExtension(PakName, ".sig"), MarkerName,
            "WahuFont_100_P.pak", "WahuFont_100_P.sig",
            "font_BeaufortforLOL-Bold_100_P.pak", "font_BeaufortforLOL-Bold_100_P.sig"
        };
        var bundledFont = FindFontPak();
        if (bundledFont is not null)
        {
            var name = Path.GetFileName(bundledFont);
            owned.Add(name);
            owned.Add(Path.ChangeExtension(name, ".sig"));
        }

        // Tự động nhận diện mọi font tiếng Việt do Tool quản lý (_100_P.pak và .sig)
        if (Directory.Exists(mods))
        {
            foreach (var file in Directory.EnumerateFiles(mods, "*", SearchOption.TopDirectoryOnly))
            {
                var fn = Path.GetFileName(file);
                if (fn.EndsWith("_100_P.pak", StringComparison.OrdinalIgnoreCase) ||
                    fn.EndsWith("_100_P.sig", StringComparison.OrdinalIgnoreCase) ||
                    fn.StartsWith("WahuFont_", StringComparison.OrdinalIgnoreCase))
                {
                    owned.Add(fn);
                }
            }
        }

        var marker = Path.Combine(mods, MarkerName);
        try
        {
            if (File.Exists(marker))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(marker));
                if (doc.RootElement.TryGetProperty("font", out var font) && !string.IsNullOrWhiteSpace(font.GetString()))
                {
                    var name = Path.GetFileName(font.GetString()!);
                    owned.Add(name);
                    owned.Add(Path.ChangeExtension(name, ".sig"));
                }
            }
        }
        catch { }
        return owned;
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
            var owned = OwnedModFiles(mods);

            if (Directory.Exists(paks))
            {
                foreach (var dir in Directory.EnumerateDirectories(paks, "~*", SearchOption.TopDirectoryOnly))
                {
                    if (Path.GetFullPath(dir).Equals(Path.GetFullPath(mods), StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
                            if ((!markerExists || !owned.Contains(Path.GetFileName(file))) && ModExtensions.Contains(Path.GetExtension(file)))
                                conflicts.Add("File mod khác: " + Path.GetRelativePath(paks, file));
                        continue;
                    }
                    foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                        if (ModExtensions.Contains(Path.GetExtension(file)))
                            conflicts.Add("Thư mục mod khác: " + Path.GetRelativePath(paks, file));
                }
            }

            var win64 = Win64Of(gamePath);
            var isWahuVersionDll = File.Exists(Path.Combine(win64, "WuWaVH.dll"))
                                || File.Exists(Path.Combine(win64, "verorg.dll"))
                                || File.Exists(Path.Combine(win64, "version_goc.dll"))
                                || (File.Exists(Path.Combine(win64, "version.dll")) && File.Exists(Path.Combine(LoaderDir, "version.dll")) && SameFile(Path.Combine(win64, "version.dll"), Path.Combine(LoaderDir, "version.dll")));

            foreach (var name in ProxyLoaders)
            {
                if (name.Equals("version.dll", StringComparison.OrdinalIgnoreCase) && isWahuVersionDll)
                    continue; // version.dll là loader của Wahu, sẽ được cài đè / nâng cấp sạch sẽ

                var installed = Path.Combine(win64, name);
                if (!File.Exists(installed)) continue;
                var ownSource = Path.Combine(LoaderDir, name);
                if (markerExists && SameFile(installed, ownSource)) continue;
                conflicts.Add("Loader/proxy khác: Win64\\" + name);
            }
            foreach (var dirName in new[] { "Mods", "ue4ss", "RE-UE4SS" })
                if (Directory.Exists(Path.Combine(win64, dirName)))
                    conflicts.Add("Bộ nạp mod khác: Win64\\" + dirName + "\\");
        }
        catch (Exception ex)
        {
            conflicts.Add("Không thể kiểm tra mod: " + ex.Message);
        }
        return conflicts.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
    }

    /// <summary>Tìm 1 file .sig gốc trong Paks\ để làm "hạt giống" cho .sig của mod (loader chỉ cần .sig TỒN TẠI).</summary>
    private static string? FindSeedSig(string paksDir)
    {
        var prefer = Path.Combine(paksDir, "pakchunk0optional-WindowsNoEditor.sig");
        if (File.Exists(prefer)) return prefer;
        try { return Directory.EnumerateFiles(paksDir, "*.sig", SearchOption.AllDirectories).FirstOrDefault(); }
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

                // Marker
                var marker = new
                {
                    variant = variant == NameVariant.English ? "en" : "hanviet",
                    font = fontName,
                    installedAt = DateTimeOffset.Now
                };
                File.WriteAllText(Path.Combine(mods, MarkerName),
                    JsonSerializer.Serialize(marker, new JsonSerializerOptions { WriteIndented = true }));
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

    public async Task<Result> UninstallAsync(string gamePath, CancellationToken ct = default)
    {
        try
        {
            var win64 = Win64Of(gamePath);
            if (IsGameRunning())
                return Result.Fail("Game hoặc launcher Wuthering Waves đang chạy. Hãy đóng hẳn game/launcher rồi mới gỡ.");

            using var operationLock = TryAcquireOperationLock(gamePath);
            if (operationLock is null)
                return Result.Fail("Đang có một tiến trình VHWuWa khác cài/gỡ. Hãy chờ tiến trình đó hoàn tất.");

            await Task.Run(() =>
            {
                // Chỉ xóa file của WAHU; giữ nguyên file lạ nếu người dùng đặt chung thư mục.
                var mods = ModsOf(gamePath);
                if (Directory.Exists(mods))
                {
                    var owned = OwnedModFiles(mods);
                    foreach (var file in Directory.EnumerateFiles(mods, "*", SearchOption.TopDirectoryOnly))
                        if (owned.Contains(Path.GetFileName(file)))
                            try { File.Delete(file); } catch { }
                    if (!Directory.EnumerateFileSystemEntries(mods).Any()) Directory.Delete(mods);
                }

                // Gỡ loader của WAHU (WuWaVH.dll, verorg.dll)
                foreach (var dll in new[] { "WuWaVH.dll", "verorg.dll" })
                {
                    var p = Path.Combine(win64, dll);
                    try { if (File.Exists(p)) File.Delete(p); } catch { }
                }
                // Khôi phục version.dll gốc nếu có backup, ngược lại xóa loader version.dll
                var ver = Path.Combine(win64, "version.dll");
                var verBak = Path.Combine(win64, "version_goc.dll");
                if (File.Exists(verBak))
                {
                    try { File.Copy(verBak, ver, true); File.Delete(verBak); } catch { }
                }
                else if (File.Exists(ver))
                {
                    try { File.Delete(ver); } catch { }
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
