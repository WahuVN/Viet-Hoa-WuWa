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
            var pkg = state.InstalledPackages.FirstOrDefault(p => p.PackageId == packageId);
            if (pkg is null) return Result.Fail("Không tìm thấy mod.");
            if (pkg.Enabled == enabled) return Result.Ok();

            if (!enabled)
            {
                // Tắt: khôi phục file gốc từ backup (gỡ lớp phủ mod)
                if (!string.IsNullOrWhiteSpace(pkg.BackupId))
                    _backup.Restore(gamePath, pkg.BackupId);
            }
            else
            {
                // Bật: copy lại từ ModCache
                var cacheDir = Path.Combine(_modCacheDir, packageId);
                foreach (var d in pkg.InstalledFiles)
                {
                    var cache = PathValidation.ResolveInsideRoot(cacheDir, d);
                    if (!File.Exists(cache)) continue;
                    var dest = PathValidation.ResolveInsideRoot(gamePath, d);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                    File.Copy(cache, dest, overwrite: true);
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
            foreach (var p in state.InstalledPackages)
                foreach (var d in p.InstalledFiles)
                    if (dests.Contains(d)) conflicts.Add($"{d} (đang thuộc {p.PackageName})");
        }
        catch { }
        return conflicts;
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

            // Xóa font pak cũ trong ~WuWaMods để tránh đè trùng font
            foreach (var old in Directory.EnumerateFiles(mods, "*", SearchOption.TopDirectoryOnly))
            {
                var fn = Path.GetFileName(old);
                if (fn.EndsWith("_100_P.pak", StringComparison.OrdinalIgnoreCase) ||
                    fn.EndsWith("_100_P.sig", StringComparison.OrdinalIgnoreCase) ||
                    fn.StartsWith("WahuFont_", StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(old); } catch { }
                }
            }

            var dest = Path.Combine(mods, Path.GetFileName(fontPakPath));
            File.Copy(fontPakPath, dest, overwrite: true);

            var seedSig = FindSeedSig(paks);
            WriteSig(seedSig, Path.ChangeExtension(dest, ".sig"));

            var markerPath = Path.Combine(mods, "vhwuwa_install.json");
            if (File.Exists(markerPath))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(markerPath));
                    var root = doc.RootElement;
                    var variant = root.TryGetProperty("variant", out var v) ? v.GetString() : "english";
                    var markerObj = new
                    {
                        schemaVersion = 1,
                        variant = variant,
                        font = Path.GetFileName(dest),
                        installedAt = DateTimeOffset.Now
                    };
                    File.WriteAllText(markerPath, System.Text.Json.JsonSerializer.Serialize(markerObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                catch { }
            }

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
            var n = 0;
            foreach (var old in Directory.EnumerateFiles(mods, "*", SearchOption.TopDirectoryOnly))
            {
                var fn = Path.GetFileName(old);
                if (fn.EndsWith("_100_P.pak", StringComparison.OrdinalIgnoreCase) ||
                    fn.EndsWith("_100_P.sig", StringComparison.OrdinalIgnoreCase) ||
                    fn.StartsWith("WahuFont_", StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(old); n++; } catch { }
                }
            }

            var markerPath = Path.Combine(mods, "vhwuwa_install.json");
            if (File.Exists(markerPath))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(markerPath));
                    var root = doc.RootElement;
                    var variant = root.TryGetProperty("variant", out var v) ? v.GetString() : "english";
                    var markerObj = new
                    {
                        schemaVersion = 1,
                        variant = variant,
                        font = (string?)null,
                        installedAt = DateTimeOffset.Now
                    };
                    File.WriteAllText(markerPath, System.Text.Json.JsonSerializer.Serialize(markerObj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
                }
                catch { }
            }

            return Task.FromResult(n > 0 ? Result.Ok() : Result.Fail("Không có font pak nào để xóa."));
        }
        catch (Exception e)
        {
            return Task.FromResult(Result.Fail(e.Message));
        }
    }

    public string? CurrentFontPak(string gamePath)
    {
        try
        {
            var mods = ModsDir(gamePath);
            if (!Directory.Exists(mods)) return null;
            return Directory.EnumerateFiles(mods, "*_100_P.pak")
                .Concat(Directory.EnumerateFiles(mods, "WahuFont_*.pak"))
                .Select(Path.GetFileName).FirstOrDefault();
        }
        catch { return null; }
    }
}
