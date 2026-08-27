using System.Reflection;
using System.Text.Json;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;

namespace VHWuWa.Infrastructure;

public sealed class UpdateService : IUpdateService
{
    private static readonly string[] DefaultReleaseEndpoints =
    {
        "https://api.github.com/repos/WahuVN/Viet-Hoa-WuWa/releases/latest"
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
        c.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
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

            // Kênh 1: Phân tích JSON từ GitHub API
            if (resp != null && resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
                var root = doc.RootElement;
                var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
                var notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
                var version = tag.TrimStart('v', 'V');
                if (string.IsNullOrWhiteSpace(version)) { res.Message = "Chưa có bản phát hành."; return res; }

                string zipUrl = "", zipSha256 = "";
                string pakEnUrl = "", pakEnSha256 = "";
                string pakHvUrl = "", pakHvSha256 = "";
                var zipPriority = -1;

                if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in assets.EnumerateArray())
                    {
                        var name = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                        var url = a.TryGetProperty("browser_download_url", out var u) ? u.GetString() ?? "" : "";

                        if (name.Equals("WuWaVH_EN_99_P.pak", StringComparison.OrdinalIgnoreCase))
                        {
                            pakEnUrl = url;
                            pakEnSha256 = ReadGitHubSha256(a);
                        }
                        else if (name.Equals("WuWaVH_HanViet_99_P.pak", StringComparison.OrdinalIgnoreCase))
                        {
                            pakHvUrl = url;
                            pakHvSha256 = ReadGitHubSha256(a);
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
                                zipSha256 = ReadGitHubSha256(a);
                            }
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(zipUrl))
                {
                    res.Message = $"Release v{version} thiếu gói VietHoa-WuWa-*.zip dành cho người chơi.";
                    return res;
                }
                if (string.IsNullOrWhiteSpace(zipSha256))
                {
                    res.Message = $"Gói cập nhật v{version} chưa có SHA-256 hợp lệ từ GitHub; đã hủy để bảo đảm an toàn.";
                    return res;
                }

                var manifest = new UpdateManifest
                {
                    Version = version,
                    ReleaseNotes = notes,
                    DownloadUrl = zipUrl,
                    Sha256 = zipSha256,
                    PakEnUrl = pakEnUrl,
                    PakEnSha256 = pakEnSha256,
                    PakHanVietUrl = pakHvUrl,
                    PakHanVietSha256 = pakHvSha256
                };

                res.CheckSucceeded = true;
                res.Manifest = manifest;
                res.UpdateAvailable = VersionComparer.IsNewer(manifest.Version, _currentVersion);
                res.Message = res.UpdateAvailable
                    ? $"Đã có phiên bản mới v{manifest.Version}!"
                    : "Bạn đang sử dụng phiên bản mới nhất.";
                return res;
            }

            // Kênh 2 (Dự phòng khi GitHub API lỗi / Rate Limit 403 / mất kết nối): Kiểm tra Redirect
            try
            {
                var redirectManifest = await CheckRedirectFallbackAsync(ct);
                if (redirectManifest != null)
                {
                    res.CheckSucceeded = true;
                    res.Manifest = redirectManifest;
                    res.UpdateAvailable = VersionComparer.IsNewer(redirectManifest.Version, _currentVersion);
                    res.Message = res.UpdateAvailable
                        ? $"Đã có phiên bản mới v{redirectManifest.Version}!"
                        : "Bạn đang sử dụng phiên bản mới nhất.";
                    return res;
                }
            }
            catch (Exception ex)
            {
                _log.Warn("Update", "Fallback redirect check lỗi: " + ex.Message);
            }

            res.Message = "Không thể kết nối máy chủ để kiểm tra cập nhật.";
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

    private async Task<UpdateManifest?> CheckRedirectFallbackAsync(CancellationToken ct)
    {
        using var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        var resp = await client.GetAsync("https://github.com/WahuVN/Viet-Hoa-WuWa/releases/latest", ct);
        var loc = resp.Headers.Location?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(loc)) return null;

        var tag = loc.Split('/').LastOrDefault()?.TrimStart('v', 'V') ?? "";
        if (string.IsNullOrWhiteSpace(tag)) return null;

        return new UpdateManifest
        {
            Version = tag,
            ReleaseNotes = "Bản cập nhật mới nhất từ GitHub.",
            DownloadUrl = $"https://github.com/WahuVN/Viet-Hoa-WuWa/releases/download/v{tag}/VietHoa-WuWa-v{tag}.zip",
            Sha256 = "",
            PakEnUrl = $"https://github.com/WahuVN/Viet-Hoa-WuWa/releases/download/v{tag}/WuWaVH_EN_99_P.pak",
            PakEnSha256 = "",
            PakHanVietUrl = $"https://github.com/WahuVN/Viet-Hoa-WuWa/releases/download/v{tag}/WuWaVH_HanViet_99_P.pak",
            PakHanVietSha256 = ""
        };
    }

    private static string ReadGitHubSha256(JsonElement asset)
    {
        if (!asset.TryGetProperty("digest", out var d)) return "";
        var digest = d.GetString() ?? "";
        const string prefix = "sha256:";
        if (!digest.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return "";
        var hex = digest[prefix.Length..].Trim();
        return hex.Length == 64 && hex.All(Uri.IsHexDigit) ? hex.ToLowerInvariant() : "";
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

            return await DownloadFileInternalAsync(manifest.DownloadUrl, manifest.Sha256, dest, progress, ct, isZip: true);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("Tải cập nhật lỗi: " + ex.Message, ex);
        }
    }

    public async Task<Result<string>> DownloadFileAsync(string fileUrl, string expectedSha256, string destinationPath,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return Result<string>.Fail("Không có đường dẫn tải tệp.");
            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

            return await DownloadFileInternalAsync(fileUrl, expectedSha256, destinationPath, progress, ct, isZip: false);
        }
        catch (Exception ex)
        {
            return Result<string>.Fail("Tải tệp lỗi: " + ex.Message, ex);
        }
    }

    private async Task<Result<string>> DownloadFileInternalAsync(string url, string expectedSha256, string dest,
        IProgress<double>? progress, CancellationToken ct, bool isZip)
    {
        using var resp = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
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

        if (!string.IsNullOrWhiteSpace(expectedSha256))
        {
            if (!_hash.Verify(dest, expectedSha256))
            {
                File.Delete(dest);
                return Result<string>.Fail("SHA-256 bản tải không khớp — đã hủy.");
            }
        }
        else if (isZip)
        {
            try
            {
                using var zipTest = System.IO.Compression.ZipFile.OpenRead(dest);
                if (!zipTest.Entries.Any())
                    throw new InvalidDataException("Tệp ZIP rỗng.");
            }
            catch (Exception ex)
            {
                File.Delete(dest);
                return Result<string>.Fail("Tệp tải về bị hỏng: " + ex.Message);
            }
        }
        else
        {
            var fi = new FileInfo(dest);
            if (fi.Length == 0)
            {
                File.Delete(dest);
                return Result<string>.Fail("Tệp tải về rỗng.");
            }
        }

        return Result<string>.Ok(dest);
    }
}
