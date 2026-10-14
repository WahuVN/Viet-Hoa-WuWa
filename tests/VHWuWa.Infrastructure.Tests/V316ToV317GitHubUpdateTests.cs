using System.Net;
using System.Text;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

/// <summary>
/// Proves the public release metadata contract expected by the shipped 3.0.16 app,
/// without publishing a release or downloading gigabytes.
/// </summary>
public sealed class V316ToV317GitHubUpdateTests
{
    private const string RealRc3Sha = "2c2e09fa6b9765c27d8ffe2e29698c35a2d5b5bc3348027127c2f803e6eba7ae";
    private const string ZipName = "VietHoa-WuWa-v3.0.17.zip";
    private const string Repository = "https://github.com/WahuVN/Viet-Hoa-WuWa/releases/download/v3.0.17/";

    [Fact]
    public async Task Shipped316_Detects317_WhenReleaseIsPublishedWithCorrectAssetAndDigest()
    {
        var release = System.Text.Json.JsonSerializer.Serialize(new {
            tag_name = "v3.0.17",
            body = "WuWa Việt Hoá PC 3.0.17 · Giao diện mới và font Lexend",
            draft = false,
            prerelease = false,
            assets = new object[] {
                new { name="App-Dich-WuWa-v3.0.17.zip", browser_download_url=Repository+"other.zip", digest="sha256:"+new string('a',64) },
                new { name="WuWaVH_EN_99_P.pak", browser_download_url=Repository+"WuWaVH_EN_99_P.pak", digest="sha256:"+new string('b',64) },
                new { name="WuWaVH_HanViet_99_P.pak", browser_download_url=Repository+"WuWaVH_HanViet_99_P.pak", digest="sha256:"+new string('c',64) },
                new { name=ZipName,browser_download_url=Repository+ZipName,digest="sha256:"+RealRc3Sha }
            }
        });
        var service = Make("3.0.16",release);
        var result = await service.CheckAsync();
        Assert.True(result.CheckSucceeded,result.Message);
        Assert.True(result.UpdateAvailable);
        Assert.NotNull(result.Manifest);
        Assert.Equal("3.0.17",result.Manifest.Version);
        Assert.Equal(Repository+ZipName,result.Manifest.DownloadUrl);
        Assert.Equal(RealRc3Sha,result.Manifest.Sha256);
        Assert.True(result.Manifest.HasDeltaPak);
        Assert.Contains("3.0.17",result.Message);
    }

    [Fact]
    public async Task Shipped316_DoesNotNotifyWhileGithubLatestIsStill316()
    {
        const string hash = "03c6bfa9f2653832a6b967e4c8d29a49e39b255147b18adc335f10f02dc3df70";
        var release = System.Text.Json.JsonSerializer.Serialize(new {
            tag_name="v3.0.16",
            assets=new[]{new{name="VietHoa-WuWa-v3.0.16.zip",browser_download_url="https://github.test/v316.zip",digest="sha256:"+hash}}
        });
        var result=await Make("3.0.16",release).CheckAsync();
        Assert.True(result.CheckSucceeded);
        Assert.False(result.UpdateAvailable);
    }

    [Fact]
    public async Task Shipped316_RefusesMisnamed317Asset()
    {
        var release=System.Text.Json.JsonSerializer.Serialize(new {
            tag_name="v3.0.17",
            assets=new[]{new{name="VietHoa-WuWa-v3.0.17-RC3-UI-TextQA.zip",browser_download_url=Repository+"wrong.zip",digest="sha256:"+RealRc3Sha}}
        });
        var result=await Make("3.0.16",release).CheckAsync();
        Assert.False(result.CheckSucceeded);
        Assert.False(result.UpdateAvailable);
        Assert.Contains(ZipName,result.Message);
    }

    [Fact]
    public async Task Shipped316_RefusesMissingSha256EvenIfVersionIsHigher()
    {
        var release=System.Text.Json.JsonSerializer.Serialize(new {
            tag_name="v3.0.17",
            assets=new[]{new{name=ZipName,browser_download_url=Repository+ZipName,digest=(string?)null}}
        });
        var result=await Make("3.0.16",release).CheckAsync();
        Assert.False(result.CheckSucceeded);
        Assert.False(result.UpdateAvailable);
        Assert.Contains("SHA-256",result.Message);
    }

    private static UpdateService Make(string version,string json)
    {
        var client=new HttpClient(new Handler(json));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VHWuWa-Updater/3.0.16");
        return new UpdateService(new QuietLog(),new HashService(),version,client,
            new[]{"https://api.github.com/repos/WahuVN/Viet-Hoa-WuWa/releases/latest"});
    }
    private sealed class Handler(string json):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
                Content=new StringContent(json,Encoding.UTF8,"application/json")
            });
    }
    private sealed class QuietLog:ILogService
    {
        public string LogDirectory=>Path.GetTempPath();
        public void Info(string operation,string message){}
        public void Warn(string operation,string message){}
        public void Error(string operation,string message,Exception? ex=null){}
        public IReadOnlyList<LogEntry> ReadRecent(int max=500,string? levelFilter=null,string? search=null)=>Array.Empty<LogEntry>();
        public void Clear(){}
    }
}
