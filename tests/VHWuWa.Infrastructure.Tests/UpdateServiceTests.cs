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
    public async Task CheckAsync_PrefersExactPlayerAssetAndUsesReleaseTag()
    {
        const string playerUrl = "https://github.test/VietHoa-WuWa-v2.1.0.zip";
        var release = """
        {
          "tag_name":"v2.1.0",
          "body":"Ghi chú từ release",
          "assets":[
            {"name":"App-Dich-WuWa-v2.1.0.zip","browser_download_url":"https://github.test/App-Dich.zip"},
            {"name":"VHWuWa-v2.1.0-win-x64.zip","browser_download_url":"https://github.test/app-only.zip"},
            {"name":"VietHoa-WuWa-v2.1.0.zip","browser_download_url":"https://github.test/VietHoa-WuWa-v2.1.0.zip"},
            {"name":"update.json","browser_download_url":"https://github.test/update.json"}
          ]
        }
        """;
        var manifest = """
        {"version":"9.9.9","downloadUrl":"https://evil.test/stale.zip","sha256":"abc","releaseNotes":"Ghi chú manifest"}
        """;
        var service = CreateService("2.0.0", request =>
            request.RequestUri!.AbsolutePath.EndsWith("update.json", StringComparison.OrdinalIgnoreCase)
                ? Json(manifest)
                : Json(release));

        var result = await service.CheckAsync();

        Assert.True(result.CheckSucceeded);
        Assert.True(result.UpdateAvailable);
        Assert.NotNull(result.Manifest);
        Assert.Equal("2.1.0", result.Manifest!.Version);
        Assert.Equal(playerUrl, result.Manifest.DownloadUrl);
        Assert.Equal("Ghi chú manifest", result.Manifest.ReleaseNotes);
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
    public async Task CheckAsync_SameVersionReportsNoUpdate()
    {
        var release = """
        {"tag_name":"v2.0.0","body":"notes","assets":[
          {"name":"VietHoa-WuWa-v2.0.0.zip","browser_download_url":"https://github.test/player.zip"}
        ]}
        """;
        var service = CreateService("2.0.0", _ => Json(release));

        var result = await service.CheckAsync();

        Assert.True(result.CheckSucceeded);
        Assert.False(result.UpdateAvailable);
        Assert.Equal("Bạn đang sử dụng phiên bản mới nhất.", result.Message);
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
