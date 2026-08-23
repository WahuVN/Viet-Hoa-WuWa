using System.Reflection;
using System.Text.Json;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;

namespace VHWuWa.Infrastructure;

public sealed class UpdateService : IUpdateService
{
    private static readonly string[] DefaultReleaseEndpoints = new[]
    {
        "https://api.github.com/repos/WahuVN/Viet-Hoa-WuWa/releases/latest",
        "https://api.github.com/repos/WahuVN/wuwa-vietnamese-launcher/releases/latest"
    };
    private readonly ILogService _log;
    private readonly IHashService _hash;
    private readonly string _currentVersion;
    private readonly HttpClient _http;
    private readonly IReadOnlyList<string> _releaseEndpoints;

    public UpdateService(ILogService log, IHashService hash, string? currentVersion = null,
        HttpClient? httpClient = null, IEnumerable<string>? releaseEndpoints = null)
    {
        _log = log; _hash = hash;
        _currentVersion = currentVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "2.0.0";
        _http = httpClient ?? CreateClient();
        _releaseEndpoints = (releaseEndpoints ?? DefaultReleaseEndpoints).ToArray();
    }

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("VHWuWa-Updater/2.0");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return c;
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var res = new UpdateCheckResult { CurrentVersion = _currentVersion };
        HttpResponseMessage? resp = null;
        try
        {
            foreach (var endpoint in _releaseEndpoints)
            {
                try
                {
                    var r = await _http.GetAsync(endpoint, ct);
                    if (r.IsSuccessStatusCode)
                    {
                        resp = r;
                        break;
                    }
                    r.Dispose();
                }
                catch { }
            }

            if (resp == null || !resp.IsSuccessStatusCode)
            {
                res.Message = "Không thể kết nối máy chủ để kiểm tra cập nhật.";
                return res;
            }

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
            var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
            var notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
            var version = tag.TrimStart('v', 'V');
            if (string.IsNullOrWhiteSpace(version)) { res.Message = "Chưa có bản phát hành."; return res; }

            string zipUrl = "", updateJsonUrl = "";
            var zipPriority = -1;
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var name = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var url = a.TryGetProperty("browser_download_url", out var u) ? u.GetString() ?? "" : "";
                    if (name.Equals("update.json", StringComparison.OrdinalIgnoreCase))
                    {
                        updateJsonUrl = url;
                    }
                    else if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
                             && !name.Contains("Community", StringComparison.OrdinalIgnoreCase)
                             && !name.Contains("App-Dich", StringComparison.OrdinalIgnoreCase))
                    {
                        var expected = $"VietHoa-WuWa-v{version}.zip";
                        var priority = name.Equals(expected, StringComparison.OrdinalIgnoreCase) ? 100
                            : name.StartsWith("VietHoa-WuWa-", StringComparison.OrdinalIgnoreCase) ? 90
                            : name.StartsWith("VHWuWa-", StringComparison.OrdinalIgnoreCase) ? 80
                            : 10;
                        if (priority > zipPriority)
                        {
                            zipPriority = priority;
                            zipUrl = url;
                        }
                    }
                }
            }

            UpdateManifest manifest;
            if (!string.IsNullOrEmpty(updateJsonUrl))
            {
                var mjson = await _http.GetStringAsync(updateJsonUrl, ct);
                manifest = VhwJson.Deserialize<UpdateManifest>(mjson) ?? new UpdateManifest();
                // Tag và asset của chính release là nguồn xác thực. Không dùng URL
                // cũ hoặc URL ngoài release còn sót trong update.json.
                manifest.Version = version;
                manifest.DownloadUrl = zipUrl;
                if (string.IsNullOrWhiteSpace(manifest.ReleaseNotes)) manifest.ReleaseNotes = notes;
            }
            else
            {
                manifest = new UpdateManifest { Version = version, ReleaseNotes = notes, DownloadUrl = zipUrl };
            }

            if (string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            {
                res.Message = $"Release v{version} thiếu gói VietHoa-WuWa-*.zip dành cho người chơi.";
                return res;
            }

            res.CheckSucceeded = true;
            res.Manifest = manifest;
            res.UpdateAvailable = VersionComparer.IsNewer(manifest.Version, _currentVersion);
            res.Message = res.UpdateAvailable
                ? $"Đã có phiên bản mới v{manifest.Version}!"
                : "Bạn đang sử dụng phiên bản mới nhất.";
            return res;
        }
        catch (Exception ex)
        {
            _log.Warn("Update", $"Kiểm tra cập nhật lỗi: {ex.Message}");
            res.Message = "Không thể kiểm tra cập nhật: " + ex.Message;
            return res;
        }
        finally
        {
            resp?.Dispose();
        }
    }

    public async Task<Result<string>> DownloadAsync(UpdateManifest manifest, string destDir,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(manifest.DownloadUrl))
                return Result<string>.Fail("Không có đường dẫn tải.");
            Directory.CreateDirectory(destDir);
            var fileName = Path.GetFileName(new Uri(manifest.DownloadUrl).LocalPath);
            if (string.IsNullOrWhiteSpace(fileName)) fileName = $"VHWuWa-{manifest.Version}.zip";
            var dest = Path.Combine(destDir, fileName);

            using var resp = await _http.GetAsync(manifest.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? -1;
            await using (var input = await resp.Content.ReadAsStreamAsync(ct))
            await using (var output = File.Create(dest))
            {
                var buffer = new byte[81920];
                long read = 0; int n;
                while ((n = await input.ReadAsync(buffer, ct)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, n), ct);
                    read += n;
                    if (total > 0) progress?.Report(read * 100.0 / total);
                }
            }

            if (!string.IsNullOrWhiteSpace(manifest.Sha256) && !_hash.Verify(dest, manifest.Sha256))
            {
                File.Delete(dest);
                return Result<string>.Fail("SHA-256 bản tải không khớp — đã hủy.");
            }
            return Result<string>.Ok(dest);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("Tải cập nhật lỗi: " + ex.Message, ex);
        }
    }
}
