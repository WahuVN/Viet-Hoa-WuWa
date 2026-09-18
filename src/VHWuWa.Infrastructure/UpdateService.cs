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
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";
        _http = httpClient ?? CreateClient(_currentVersion);
        _releaseEndpoints = (releaseEndpoints ?? DefaultReleaseEndpoints).ToArray();
    }

    private static HttpClient CreateClient(string version)
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd($"VHWuWa-Updater/{version}");
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
                var expectedPlayerAsset = $"VietHoa-WuWa-v{version}.zip";

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
                        else if (name.Equals(expectedPlayerAsset, StringComparison.OrdinalIgnoreCase))
                        {
                            zipUrl = url;
                            zipSha256 = ReadGitHubSha256(a);
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(zipUrl))
                {
                    res.Message = $"Release v{version} thiếu gói bắt buộc {expectedPlayerAsset}.";
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

    public async Task<Result<UpdateManifest>> GetReleaseManifestAsync(string version, CancellationToken ct = default)
    {
        try
        {
            if (!Version.TryParse(version, out var parsed))
                return Result<UpdateManifest>.Fail("Phiên bản release không hợp lệ.");

            var normalized = new Version(parsed.Major, Math.Max(0, parsed.Minor), Math.Max(0, parsed.Build)).ToString();
            var endpoint = "https://api.github.com/repos/WahuVN/Viet-Hoa-WuWa/releases/tags/v"
                + Uri.EscapeDataString(normalized);
            using var resp = await _http.GetAsync(endpoint, ct);
            if (!resp.IsSuccessStatusCode)
                return Result<UpdateManifest>.Fail($"Không tìm thấy release v{normalized}.");

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
            var tag = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
            var actualVersion = tag.TrimStart('v', 'V');
            if (!Version.TryParse(actualVersion, out var actualParsed)
                || new Version(actualParsed.Major, Math.Max(0, actualParsed.Minor), Math.Max(0, actualParsed.Build)).ToString() != normalized)
                return Result<UpdateManifest>.Fail($"Release trả về sai phiên bản: cần v{normalized}, nhận '{tag}'.");

            string zipUrl = "", zipSha256 = "";
            string pakEnUrl = "", pakEnSha256 = "";
            string pakHvUrl = "", pakHvSha256 = "";
            var expectedPlayerAsset = $"VietHoa-WuWa-v{normalized}.zip";
            if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var url = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() ?? "" : "";
                    if (name.Equals("WuWaVH_EN_99_P.pak", StringComparison.OrdinalIgnoreCase))
                    {
                        pakEnUrl = url;
                        pakEnSha256 = ReadGitHubSha256(asset);
                    }
                    else if (name.Equals("WuWaVH_HanViet_99_P.pak", StringComparison.OrdinalIgnoreCase))
                    {
                        pakHvUrl = url;
                        pakHvSha256 = ReadGitHubSha256(asset);
                    }
                    else if (name.Equals(expectedPlayerAsset, StringComparison.OrdinalIgnoreCase))
                    {
                        zipUrl = url;
                        zipSha256 = ReadGitHubSha256(asset);
                    }
                }
            }

            var notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
            return Result<UpdateManifest>.Ok(new UpdateManifest
            {
                Version = normalized,
                ReleaseNotes = notes,
                DownloadUrl = zipUrl,
                Sha256 = zipSha256,
                PakEnUrl = pakEnUrl,
                PakEnSha256 = pakEnSha256,
                PakHanVietUrl = pakHvUrl,
                PakHanVietSha256 = pakHvSha256
            });
        }
        catch (Exception ex)
        {
            _log.Warn("Update", $"Đọc release theo phiên bản lỗi: {ex.Message}");
            return Result<UpdateManifest>.Fail("Không đọc được release theo phiên bản: " + ex.Message, ex);
        }
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
            if (!Version.TryParse(manifest.Version, out _))
                return Result<string>.Fail("Phiên bản cập nhật không hợp lệ.");
            if (manifest.Sha256.Length != 64 || !manifest.Sha256.All(Uri.IsHexDigit))
                return Result<string>.Fail("Gói cập nhật thiếu SHA-256 hợp lệ.");
            Directory.CreateDirectory(destDir);
            var fileName = $"VietHoa-WuWa-v{manifest.Version}.zip";
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
        if (!Uri.TryCreate(url, UriKind.Absolute, out var downloadUri)
            || downloadUri.Scheme != Uri.UriSchemeHttps)
            return Result<string>.Fail("Đường dẫn tải phải là HTTPS hợp lệ.");

        var partial = dest + ".part-" + Guid.NewGuid().ToString("N");
        try
        {
            using var resp = await _http.GetAsync(downloadUri, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            var total = resp.Content.Headers.ContentLength ?? -1;
            await using (var input = await resp.Content.ReadAsStreamAsync(ct))
            await using (var output = File.Create(partial))
            {
                var buffer = new byte[81920];
                long read = 0;
                int count;
                while ((count = await input.ReadAsync(buffer, ct)) > 0)
                {
                    await output.WriteAsync(buffer.AsMemory(0, count), ct);
                    read += count;
                    if (total > 0) progress?.Report(read * 100.0 / total);
                }
            }

            if (!string.IsNullOrWhiteSpace(expectedSha256))
            {
                if (!_hash.Verify(partial, expectedSha256))
                    return Result<string>.Fail("SHA-256 bản tải không khớp — đã hủy.");
            }
            else if (isZip)
            {
                return Result<string>.Fail("Gói ZIP cập nhật thiếu SHA-256 hợp lệ.");
            }
            else
            {
                var fi = new FileInfo(partial);
                if (fi.Length == 0)
                    return Result<string>.Fail("Tệp tải về rỗng.");
            }

            File.Move(partial, dest, overwrite: true);
            progress?.Report(100);
            return Result<string>.Ok(dest);
        }
        finally
        {
            try { if (File.Exists(partial)) File.Delete(partial); } catch { }
        }
    }
}
