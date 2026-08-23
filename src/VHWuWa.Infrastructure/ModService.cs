using System.Diagnostics;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;

namespace VHWuWa.Infrastructure;

public sealed class ModService : IModService
{
    private readonly ISettingsService _settings;
    private readonly IBackupService _backup;
    private readonly ILogService _log;
    private readonly string _modCacheDir;

    public ModService(ISettingsService settings, IBackupService backup, ILogService log)
    {
        _settings = settings; _backup = backup; _log = log;
        _modCacheDir = Path.Combine(settings.AppDataDirectory, "ModCache");
    }

    public IReadOnlyList<ModInfo> ListInstalled(string gamePath)
    {
        var state = _settings.LoadState();
        var list = new List<ModInfo>();
        foreach (var p in state.InstalledPackages.Where(p => p.PackageType == PackageType.Mod))
        {
            long size = 0;
            foreach (var d in p.InstalledFiles)
            {
                try
                {
                    var full = PathValidation.ResolveInsideRoot(gamePath, d);
                    if (File.Exists(full)) size += new FileInfo(full).Length;
                }
                catch { }
            }
            var conflicts = state.InstalledPackages
                .Where(o => o.PackageId != p.PackageId)
                .Where(o => o.InstalledFiles.Intersect(p.InstalledFiles, StringComparer.OrdinalIgnoreCase).Any())
                .Select(o => o.PackageName)
                .ToList();
            list.Add(new ModInfo
            {
                PackageId = p.PackageId, Name = p.PackageName, Version = p.Version,
                InstalledAt = p.InstalledAt, Enabled = p.Enabled, Installed = true,
                SizeBytes = size, Conflicts = conflicts,
            });
        }
        return list;
    }

    public Result SetEnabled(string gamePath, string packageId, bool enabled)
    {
        try
        {
            var state = _settings.LoadState();
            var pkg = state.InstalledPackages.FirstOrDefault(p =>
                p.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));
            if (pkg is null) return Result.Fail("Không tìm thấy mod.");
            if (pkg.Enabled == enabled) return Result.Ok();

            var overlaps = state.InstalledPackages
                .Where(p => p.Enabled && !p.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase))
                .Where(p => p.InstalledFiles.Intersect(pkg.InstalledFiles, StringComparer.OrdinalIgnoreCase).Any())
                .Select(p => p.PackageName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (enabled && overlaps.Count > 0)
                return Result.Fail($"Mod xung đột với: {string.Join(", ", overlaps)}. Hãy tắt/gỡ mod đó trước.");

            if (!enabled)
            {
                // Tắt: khôi phục file gốc từ backup (gỡ lớp phủ mod)
                var changed = ChangedFiles(gamePath, pkg);
                if (changed.Count > 0)
                    return Result.Fail("Không tắt để tránh ghi đè file đã bị mod khác thay đổi:\n- "
                        + string.Join("\n- ", changed));
                if (string.IsNullOrWhiteSpace(pkg.BackupId))
                    return Result.Fail("Mod không có thông tin backup nên không thể tắt an toàn.");
                var restore = _backup.Restore(gamePath, pkg.BackupId);
                if (!restore.Success) return restore;
            }
            else
            {
                // Bật: copy lại từ ModCache
                var cacheDir = Path.Combine(_modCacheDir, pkg.PackageId);
                if (!Directory.Exists(cacheDir))
                    return Result.Fail("Đã mất bộ nhớ đệm của mod; hãy cài lại gói thay vì bật.");
                var missing = pkg.InstalledFiles.Where(d =>
                    !File.Exists(PathValidation.ResolveInsideRoot(cacheDir, d))).ToList();
                if (missing.Count > 0)
                    return Result.Fail("Thiếu file bộ nhớ đệm; chưa bật mod và chưa thay đổi game:\n- "
                        + string.Join("\n- ", missing));
                foreach (var d in pkg.InstalledFiles)
                {
                    var cache = PathValidation.ResolveInsideRoot(cacheDir, d);
                    var dest = PathValidation.ResolveInsideRoot(gamePath, d);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(cache, dest, overwrite: true);
                    pkg.FileHashes ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    pkg.FileHashes[d] = ViethoaInstallMarker.Sha256(cache);
                }
            }
            pkg.Enabled = enabled;
            _settings.SaveState(state);
            _log.Info("Mod", $"{(enabled ? "Bật" : "Tắt")} mod '{packageId}'.");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            _log.Error("Mod", $"Đổi trạng thái mod thất bại: {ex.Message}", ex);
            return Result.Fail("Thất bại: " + ex.Message, ex);
        }
    }

    public IReadOnlyList<string> DetectConflicts(string vhwpackPath)
    {
        var conflicts = new List<string>();
        try
        {
            using var reader = VhwPackageReader.Open(vhwpackPath);
            var dests = reader.Manifest.Files.Select(f => f.Destination).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var state = _settings.LoadState();
            foreach (var p in state.InstalledPackages.Where(p => p.Enabled
                         && !p.PackageId.Equals(reader.Manifest.PackageId, StringComparison.OrdinalIgnoreCase)))
                foreach (var d in p.InstalledFiles)
                    if (dests.Contains(d)) conflicts.Add($"{p.PackageName}: {d}");
        }
        catch { }
        return conflicts;
    }

    private List<string> ChangedFiles(string gamePath, InstalledPackage package)
    {
        var changed = new List<string>();
        var cacheDir = Path.Combine(_modCacheDir, package.PackageId);
        foreach (var destination in package.InstalledFiles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var installed = PathValidation.ResolveInsideRoot(gamePath, destination);
            if (!File.Exists(installed)) continue;
            var expected = (package.FileHashes ?? new Dictionary<string, string>())
                .FirstOrDefault(x => x.Key.Equals(destination, StringComparison.OrdinalIgnoreCase)).Value;
            if (string.IsNullOrWhiteSpace(expected))
            {
                var cache = PathValidation.ResolveInsideRoot(cacheDir, destination);
                if (File.Exists(cache)) expected = ViethoaInstallMarker.Sha256(cache);
            }
            if (!string.IsNullOrWhiteSpace(expected)
                && !expected.Equals(ViethoaInstallMarker.Sha256(installed), StringComparison.OrdinalIgnoreCase))
                changed.Add(destination);
        }
        return changed;
    }
}

public sealed class FontService : IFontService
{
    private readonly IPackageInstallerService _installer;
    private readonly ISettingsService _settings;

    public FontService(IPackageInstallerService installer, ISettingsService settings)
    {
        _installer = installer; _settings = settings;
    }

    public Task<Result> ApplyFontAsync(string gamePath, string vhwpackPath, CancellationToken ct = default)
        => _installer.InstallAsync(gamePath, vhwpackPath, null, ct);

    public async Task<Result> RestoreDefaultAsync(string gamePath, CancellationToken ct = default)
    {
        var state = _settings.LoadState();
        var fonts = state.InstalledPackages.Where(p => p.PackageType == PackageType.Font).ToList();
        if (fonts.Count == 0) return Result.Fail("Chưa có font nào được cài.");
        foreach (var f in fonts)
        {
            var r = await _installer.UninstallAsync(gamePath, f.PackageId, ct);
            if (!r.Success) return r;
        }
        return Result.Ok();
    }

    private static string ModsDir(string gamePath)
        => Path.Combine(gamePath, "Client", "Content", "Paks", "~WuWaMods");

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
        else File.WriteAllBytes(dstSig, Array.Empty<byte>());
    }

    public Task<Result> ApplyFontPakAsync(string gamePath, string fontPakPath, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
                return Task.FromResult(Result.Fail("Chưa chọn thư mục game hợp lệ."));
            if (!File.Exists(fontPakPath))
                return Task.FromResult(Result.Fail("Không thấy file font: " + fontPakPath));

            var paks = Path.Combine(gamePath, "Client", "Content", "Paks");
            if (!Directory.Exists(paks))
                return Task.FromResult(Result.Fail("Không thấy thư mục Client\\Content\\Paks của game. Hãy chọn đúng thư mục game."));

            var mods = ModsDir(gamePath);
            Directory.CreateDirectory(mods);
            var markerPath = Path.Combine(mods, "vhwuwa_install.json");
            var marker = ViethoaInstallMarker.Load(markerPath);
            if (marker is null)
                return Task.FromResult(Result.Fail("Chưa có bản Việt hóa do VHWuWa quản lý. Hãy cài Việt hóa trước khi đổi font."));
            var newFontName = Path.GetFileName(fontPakPath);
            if (newFontName.Equals("WuWaVH_99_P.pak", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(Result.Fail("Tên file font trùng với PAK Việt hóa; hãy chọn file font khác."));

            // Chỉ xóa đúng font đã được marker ghi nhận; không đụng tới mod *_100_P.pak khác.
            var oldFont = string.IsNullOrWhiteSpace(marker.Font) ? null : Path.GetFileName(marker.Font);
            if (oldFont is not null)
            {
                var oldPak = Path.Combine(mods, oldFont);
                var oldSig = Path.Combine(mods, Path.ChangeExtension(oldFont, ".sig"));
                if (File.Exists(oldPak) && !marker.Matches("mods", oldPak))
                    return Task.FromResult(Result.Fail("Font hiện tại đã bị mod khác ghi đè; không thay thế để tránh xóa nhầm. Hãy gỡ mod xung đột trước."));
                if (File.Exists(oldSig) && !marker.Matches("mods", oldSig))
                    return Task.FromResult(Result.Fail("File chữ ký của font đã bị mod khác thay đổi; không thay thế để tránh xóa nhầm."));
                if (File.Exists(oldPak)) File.Delete(oldPak);
                if (File.Exists(oldSig)) File.Delete(oldSig);
                marker.ForgetModFile(oldFont);
                marker.ForgetModFile(Path.ChangeExtension(oldFont, ".sig"));
            }

            var dest = Path.Combine(mods, newFontName);
            File.Copy(fontPakPath, dest, overwrite: true);

            var seedSig = FindSeedSig(paks);
            var destSig = Path.ChangeExtension(dest, ".sig");
            WriteSig(seedSig, destSig);
            marker.Font = Path.GetFileName(dest);
            marker.Track("mods", dest);
            marker.Track("mods", destSig);
            marker.Save(markerPath);

            return Task.FromResult(Result.Ok());
        }
        catch (Exception e)
        {
            return Task.FromResult(Result.Fail(e.Message));
        }
    }

    public Task<Result> RemoveFontPaksAsync(string gamePath, CancellationToken ct = default)
    {
        try
        {
            var mods = ModsDir(gamePath);
            if (!Directory.Exists(mods)) return Task.FromResult(Result.Ok());
            var markerPath = Path.Combine(mods, "vhwuwa_install.json");
            var marker = ViethoaInstallMarker.Load(markerPath);
            var font = string.IsNullOrWhiteSpace(marker?.Font) ? null : Path.GetFileName(marker.Font);
            if (marker is null || font is null)
                return Task.FromResult(Result.Fail("Không có font do VHWuWa quản lý để xóa."));

            var pak = Path.Combine(mods, font);
            var sig = Path.Combine(mods, Path.ChangeExtension(font, ".sig"));
            if (File.Exists(pak) && !marker.Matches("mods", pak))
                return Task.FromResult(Result.Fail("Font đã bị mod khác thay đổi; không xóa để tránh mất dữ liệu. Hãy gỡ mod ghi đè trước."));
            if (File.Exists(sig) && !marker.Matches("mods", sig))
                return Task.FromResult(Result.Fail("File chữ ký của font đã bị mod khác thay đổi; không xóa để tránh mất dữ liệu."));
            if (File.Exists(pak)) File.Delete(pak);
            if (File.Exists(sig)) File.Delete(sig);
            marker.ForgetModFile(font);
            marker.ForgetModFile(Path.ChangeExtension(font, ".sig"));
            marker.Font = null;
            marker.Save(markerPath);
            return Task.FromResult(Result.Ok());
        }
        catch (Exception e)
        {
            return Task.FromResult(Result.Fail(e.Message));
        }
    }

    public async Task<Result> BuildAndApplyCustomFontAsync(string gamePath, string rawFontPath, CancellationToken ct = default)
    {
        string? tempDir = null;
        try
        {
            if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
                return Result.Fail("Chưa chọn thư mục game hợp lệ.");
            if (!File.Exists(rawFontPath))
                return Result.Fail("Không tìm thấy tệp font: " + rawFontPath);

            var repakCandidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "tools", "repak.exe"),
                Path.Combine(AppContext.BaseDirectory, "repak.exe"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "tools", "repak.exe"))
            };
            var repak = repakCandidates.FirstOrDefault(File.Exists);
            if (repak == null)
            {
                return Result.Fail("Không tìm thấy công cụ đóng gói repak.exe. Hãy kiểm tra thư mục tools\\.");
            }

            tempDir = Path.Combine(Path.GetTempPath(), "VHWuWa_Font_" + Guid.NewGuid().ToString("N"));
            var contentDir = Path.Combine(tempDir, "content");
            var stagingFontDir = Path.Combine(contentDir, "Client", "Content", "Aki", "UI", "Framework", "LGUI", "Font");
            Directory.CreateDirectory(stagingFontDir);

            var destUfont = Path.Combine(stagingFontDir, "LaguSansBold.ufont");
            File.Copy(rawFontPath, destUfont, true);

            var fontName = Path.GetFileNameWithoutExtension(rawFontPath);
            var safeName = System.Text.RegularExpressions.Regex.Replace(fontName, @"[^\w\-]", "_");
            var outPak = Path.Combine(tempDir, $"{safeName}_100_P.pak");
            var v11Pak = outPak + ".v11.tmp";

            var psi = new ProcessStartInfo
            {
                FileName = repak,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            psi.ArgumentList.Add("pack");
            psi.ArgumentList.Add("--version");
            psi.ArgumentList.Add("V11");
            psi.ArgumentList.Add("--mount-point");
            psi.ArgumentList.Add("../../../");
            psi.ArgumentList.Add(contentDir);
            psi.ArgumentList.Add(v11Pak);

            using var p = Process.Start(psi);
            if (p == null) return Result.Fail("Không thể khởi chạy repak.exe");
            var stdoutTask = p.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (p.ExitCode != 0 || !File.Exists(v11Pak))
            {
                var details = string.Join(" ", new[] { stdout.Trim(), stderr.Trim() }.Where(s => s.Length > 0));
                return Result.Fail("repak không thể đóng gói font V11" + (details.Length > 0 ? ": " + details : "."));
            }

            PakV12Converter.ConvertV11ToV12(v11Pak, outPak);
            if (!PakV12Converter.TryVerifyV12(outPak, out var verifyError))
                return Result.Fail("Gói font V12 không hợp lệ: " + verifyError);

            var applyRes = await ApplyFontPakAsync(gamePath, outPak, ct);
            return applyRes;
        }
        catch (Exception ex)
        {
            return Result.Fail("Lỗi khi tạo font: " + ex.Message);
        }
        finally
        {
            if (tempDir is not null)
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }

    public string? CurrentFontPak(string gamePath)
    {
        try
        {
            var mods = ModsDir(gamePath);
            if (!Directory.Exists(mods)) return null;
            var marker = ViethoaInstallMarker.Load(Path.Combine(mods, "vhwuwa_install.json"));
            if (string.IsNullOrWhiteSpace(marker?.Font)) return null;
            var name = Path.GetFileName(marker.Font);
            return File.Exists(Path.Combine(mods, name)) ? name : null;
        }
        catch { return null; }
    }
}
