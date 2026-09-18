using System.Net;
using System.Text;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class UpdateServiceTests
{
    [Fact]
    public async Task CheckAsync_PrefersExactPlayerAssetAndUsesGitHubDigest()
    {
        const string playerUrl = "https://github.test/VietHoa-WuWa-v2.1.0.zip";
        const string playerSha = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var release = """
        {
          "tag_name":"v2.1.0",
          "body":"Ghi chú từ release",
          "assets":[
            {"name":"App-Dich-WuWa-v2.1.0.zip","browser_download_url":"https://github.test/App-Dich.zip","digest":"sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"},
            {"name":"VHWuWa-v2.1.0-win-x64.zip","browser_download_url":"https://github.test/app-only.zip","digest":"sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"},
            {"name":"VietHoa-WuWa-v2.1.0.zip","browser_download_url":"https://github.test/VietHoa-WuWa-v2.1.0.zip","digest":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}
          ]
        }
        """;
        var service = CreateService("2.0.0", _ => Json(release));

        var result = await service.CheckAsync();

        Assert.True(result.CheckSucceeded);
        Assert.True(result.UpdateAvailable);
        Assert.NotNull(result.Manifest);
        Assert.Equal("2.1.0", result.Manifest!.Version);
        Assert.Equal(playerUrl, result.Manifest.DownloadUrl);
        Assert.Equal(playerSha, result.Manifest.Sha256);
        Assert.Equal("Ghi chú từ release", result.Manifest.ReleaseNotes);
    }

    [Fact]
    public async Task CheckAsync_DoesNotClaimLatestWhenPlayerZipIsMissing()
    {
        var release = """
        {"tag_name":"v2.1.0","body":"notes","assets":[
          {"name":"App-Dich-WuWa-v2.1.0.zip","browser_download_url":"https://github.test/App-Dich.zip"}
        ]}
        """;
        var service = CreateService("2.0.0", _ => Json(release));

        var result = await service.CheckAsync();

        Assert.False(result.CheckSucceeded);
        Assert.False(result.UpdateAvailable);
        Assert.Contains("thiếu gói", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckAsync_309_DetectsExact310PlayerRelease()
    {
        const string playerUrl = "https://github.test/VietHoa-WuWa-v3.0.10.zip";
        const string playerSha = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
        var release = """
        {"tag_name":"v3.0.10","body":"Bản 3.0.10","assets":[
          {"name":"VietHoa-WuWa-v3.0.10.zip","browser_download_url":"https://github.test/VietHoa-WuWa-v3.0.10.zip","digest":"sha256:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789"}
        ]}
        """;
        var service = CreateService("3.0.9", _ => Json(release));

        var result = await service.CheckAsync();

        Assert.True(result.CheckSucceeded);
        Assert.True(result.UpdateAvailable);
        Assert.NotNull(result.Manifest);
        Assert.Equal("3.0.9", result.CurrentVersion);
        Assert.Equal("3.0.10", result.Manifest!.Version);
        Assert.Equal(playerUrl, result.Manifest.DownloadUrl);
        Assert.Equal(playerSha, result.Manifest.Sha256);
    }

    [Fact]
    public async Task CheckAsync_RejectsSimilarButWrongPlayerAssetName()
    {
        var release = """
        {"tag_name":"v3.0.10","body":"notes","assets":[
          {"name":"VietHoa-WuWa-v3.0.9.zip","browser_download_url":"https://github.test/old.zip","digest":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"},
          {"name":"VHWuWa-v3.0.10-win-x64.zip","browser_download_url":"https://github.test/app-only.zip","digest":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}
        ]}
        """;
        var service = CreateService("3.0.9", _ => Json(release));

        var result = await service.CheckAsync();

        Assert.False(result.CheckSucceeded);
        Assert.False(result.UpdateAvailable);
        Assert.Contains("VietHoa-WuWa-v3.0.10.zip", result.Message);
    }

    [Fact]
    public async Task CheckAsync_SameVersionReportsNoUpdate()
    {
        var release = """
        {"tag_name":"v2.0.0","body":"notes","assets":[
          {"name":"VietHoa-WuWa-v2.0.0.zip","browser_download_url":"https://github.test/player.zip","digest":"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"}
        ]}
        """;
        var service = CreateService("2.0.0", _ => Json(release));

        var result = await service.CheckAsync();

        Assert.True(result.CheckSucceeded);
        Assert.False(result.UpdateAvailable);
        Assert.Equal("Bạn đang sử dụng phiên bản mới nhất.", result.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sha256:abc")]
    [InlineData("md5:0123456789abcdef0123456789abcdef")]
    public async Task CheckAsync_RejectsPlayerAssetWithoutValidGitHubSha256(string digest)
    {
        var release = $$"""
        {"tag_name":"v2.1.0","body":"notes","assets":[
          {"name":"VietHoa-WuWa-v2.1.0.zip","browser_download_url":"https://github.test/player.zip","digest":"{{digest}}"}
        ]}
        """;
        var service = CreateService("2.0.0", _ => Json(release));

        var result = await service.CheckAsync();

        Assert.False(result.CheckSucceeded);
        Assert.False(result.UpdateAvailable);
        Assert.Contains("SHA-256", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadAsync_VerifiesSha256AndDeletesCorruptDownload()
    {
        var bytes = Encoding.UTF8.GetBytes("valid update archive");
        var hash = new HashService();
        var expected = hash.Sha256Bytes(bytes);
        var service = CreateService("2.0.0", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
        });
        var temp = Path.Combine(Path.GetTempPath(), "VHWuWa_DownloadTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var good = await service.DownloadAsync(new UpdateManifest
            {
                Version = "2.1.0",
                DownloadUrl = "https://github.test/VietHoa-WuWa-v2.1.0.zip",
                Sha256 = expected,
            }, Path.Combine(temp, "good"));
            var bad = await service.DownloadAsync(new UpdateManifest
            {
                Version = "2.1.0",
                DownloadUrl = "https://github.test/VietHoa-WuWa-v2.1.0.zip",
                Sha256 = new string('0', 64),
            }, Path.Combine(temp, "bad"));

            Assert.True(good.Success);
            Assert.True(File.Exists(good.Value));
            Assert.False(bad.Success);
            Assert.Empty(Directory.GetFiles(Path.Combine(temp, "bad")));
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadAsync_RejectsUpdate_WhenSha256IsEmpty()
    {
        // Tạo một tệp ZIP hợp lệ trong bộ nhớ
        byte[] validZipBytes;
        using (var ms = new MemoryStream())
        {
            using (var archive = new System.IO.Compression.ZipArchive(ms, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                var entry = archive.CreateEntry("VHWuWa.exe");
                using var writer = new StreamWriter(entry.Open());
                writer.Write("dummy exe content");
            }
            validZipBytes = ms.ToArray();
        }

        var service = CreateService("2.0.0", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(validZipBytes)
        });

        var temp = Path.Combine(Path.GetTempPath(), "VHWuWa_ZipTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var res = await service.DownloadAsync(new UpdateManifest
            {
                Version = "2.1.0",
                DownloadUrl = "https://github.test/VietHoa-WuWa-v2.1.0.zip",
                Sha256 = "", // Không có SHA256 (fallback mode)
            }, temp);

            Assert.False(res.Success);
            Assert.Contains("SHA-256", res.Error, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(Directory.Exists(temp) ? Directory.GetFiles(temp) : Array.Empty<string>());
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task DownloadAsync_RejectsBeforeDownloading_WhenSha256IsEmpty()
    {
        var corruptBytes = Encoding.UTF8.GetBytes("not a valid zip file content");
        var service = CreateService("2.0.0", _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(corruptBytes)
        });

        var temp = Path.Combine(Path.GetTempPath(), "VHWuWa_CorruptZipTest_" + Guid.NewGuid().ToString("N"));
        try
        {
            var res = await service.DownloadAsync(new UpdateManifest
            {
                Version = "2.1.0",
                DownloadUrl = "https://github.test/VietHoa-WuWa-v2.1.0.zip",
                Sha256 = "",
            }, temp);

            Assert.False(res.Success);
            Assert.Contains("SHA-256", res.Error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public async Task GetReleaseManifestAsync_UsesExactTagAndReadsHanVietDigest()
    {
        const string hvUrl = "https://github.test/WuWaVH_HanViet_99_P.pak";
        const string hvSha = "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789";
        string? requested = null;
        var release = $$"""
        {"tag_name":"v3.0.9","body":"notes","assets":[
          {"name":"WuWaVH_HanViet_99_P.pak","browser_download_url":"{{hvUrl}}","digest":"sha256:{{hvSha}}"}
        ]}
        """;
        var service = CreateService("3.0.9", request =>
        {
            requested = request.RequestUri?.AbsoluteUri;
            return Json(release);
        });

        var result = await service.GetReleaseManifestAsync("3.0.9");

        Assert.True(result.Success, result.Error);
        Assert.NotNull(result.Value);
        Assert.Equal("3.0.9", result.Value!.Version);
        Assert.Equal(hvUrl, result.Value.PakHanVietUrl);
        Assert.Equal(hvSha, result.Value.PakHanVietSha256);
        Assert.Contains("/releases/tags/v3.0.9", requested, StringComparison.Ordinal);
        Assert.DoesNotContain("/latest", requested, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetReleaseManifestAsync_RejectsMismatchedReturnedTag()
    {
        string? requested = null;
        var release = """
        {"tag_name":"v3.0.10","body":"notes","assets":[]}
        """;
        var service = CreateService("3.0.9", request =>
        {
            requested = request.RequestUri?.AbsoluteUri;
            return Json(release);
        });

        var result = await service.GetReleaseManifestAsync("3.0.9");

        Assert.False(result.Success);
        Assert.Contains("sai", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/releases/tags/v3.0.9", requested, StringComparison.Ordinal);
    }

    private static UpdateService CreateService(string currentVersion,
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var client = new HttpClient(new StubHandler(responder));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VHWuWa-Test/1.0");
        return new UpdateService(new NullLog(), new HashService(), currentVersion, client,
            new[] { "https://api.github.test/releases/latest" });
    }

    private static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responder(request));
    }

    private sealed class NullLog : ILogService
    {
        public string LogDirectory => Path.GetTempPath();
        public void Info(string operation, string message) { }
        public void Warn(string operation, string message) { }
        public void Error(string operation, string message, Exception? ex = null) { }
        public IReadOnlyList<LogEntry> ReadRecent(int max = 500, string? levelFilter = null,
            string? search = null) => Array.Empty<LogEntry>();
        public void Clear() { }
    }
}
