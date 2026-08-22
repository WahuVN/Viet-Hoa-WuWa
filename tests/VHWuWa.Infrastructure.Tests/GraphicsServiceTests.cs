using VHWuWa.Core.Models;
using VHWuWa.Core.Services;
using VHWuWa.Infrastructure;
using Xunit;

namespace VHWuWa.Infrastructure.Tests;

public sealed class GraphicsServiceTests : IDisposable
{
    private readonly string _work = Path.Combine(Path.GetTempPath(), "vhw_gfx_" + Guid.NewGuid().ToString("N"));
    private readonly string _game;
    private readonly string _config;
    private readonly GraphicsService _gfx;
    private readonly LogService _log;
    private readonly string _iniPath;

    public GraphicsServiceTests()
    {
        _game = Path.Combine(_work, "game");
        _config = Path.Combine(_work, "Config");
        Directory.CreateDirectory(_config);

        // Engine.ini nằm ĐÚNG chỗ WuWa: Client\Saved\Config\WindowsNoEditor\
        _iniPath = Path.Combine(_game, "Client", "Saved", "Config", "WindowsNoEditor", "Engine.ini");
        Directory.CreateDirectory(Path.GetDirectoryName(_iniPath)!);
        File.WriteAllText(_iniPath, "[Core.System]\nPaths=x\n");

        var cfg = new GraphicsConfig
        {
            ConfigFormat = "ini",
            ConfigPath = "Client/Saved/Config/WindowsNoEditor/Engine.ini",
            Options =
            {
                new GraphicsOption { Key = "grass.DensityScale", Label = "Cỏ", Section = "SystemSettings", Choices = { "0", "1" } },
                new GraphicsOption { Key = "foliage.DensityScale", Label = "Lá", Section = "SystemSettings", Choices = { "0", "1" } },
                new GraphicsOption { Key = "r.MotionBlurQuality", Label = "MB", Section = "SystemSettings", Choices = { "0", "4" } },
            },
            Presets =
            {
                new GraphicsPreset { Name = "Xóa cỏ / Max FPS", Values = new()
                    { ["grass.DensityScale"] = "0", ["foliage.DensityScale"] = "0", ["r.MotionBlurQuality"] = "0" } }
            }
        };
        File.WriteAllText(Path.Combine(_config, "graphics.json"), VhwJson.Serialize(cfg));

        var settings = new SettingsService(Path.Combine(_work, "appdata"));
        _log = new LogService(settings);
        var backup = new BackupService(settings, _log, new HashService());
        _gfx = new GraphicsService(backup, _log, _config);
    }

    [Fact]
    public void ConfigPath_Points_Inside_Client_Saved()
    {
        Assert.True(_gfx.IsSupported);
        var p = _gfx.ConfigFilePath(_game);
        Assert.NotNull(p);
        Assert.Contains(Path.Combine("Client", "Saved", "Config", "WindowsNoEditor", "Engine.ini"), p!);
    }

    [Fact]
    public void ApplyPreset_XoaCo_Writes_Grass_Zero()
    {
        var r = _gfx.ApplyPreset(_game, "Xóa cỏ / Max FPS");
        Assert.True(r.Success, r.Error);

        var ini = File.ReadAllText(_iniPath);
        Assert.Contains("[SystemSettings]", ini);
        Assert.Contains("grass.DensityScale=0", ini);
        Assert.Contains("foliage.DensityScale=0", ini);
        Assert.Contains("r.MotionBlurQuality=0", ini);
        // Giữ nguyên nội dung cũ
        Assert.Contains("[Core.System]", ini);

        var cur = _gfx.ReadCurrent(_game);
        Assert.Equal("0", cur["grass.DensityScale"]);
    }

    [Fact]
    public void Apply_Then_Update_Same_Key_NoDuplicate()
    {
        _gfx.Apply(_game, new() { ["grass.DensityScale"] = "1" });
        _gfx.Apply(_game, new() { ["grass.DensityScale"] = "0" });
        var ini = File.ReadAllText(_iniPath);
        // chỉ còn 1 dòng grass.DensityScale (đã cập nhật, không nhân đôi)
        var count = ini.Split('\n').Count(l => l.TrimStart().StartsWith("grass.DensityScale="));
        Assert.Equal(1, count);
        Assert.Contains("grass.DensityScale=0", ini);
    }

    [Fact]
    public void Lock_Unlock_And_Apply_While_Locked()
    {
        // Ghi lần đầu để có file
        _gfx.Apply(_game, new() { ["grass.DensityScale"] = "1" });
        Assert.False(_gfx.IsReadOnly(_game));

        // Khóa
        Assert.True(_gfx.SetReadOnly(_game, true).Success);
        Assert.True(_gfx.IsReadOnly(_game));

        // Áp cấu hình khi đang khóa → tự bỏ khóa để ghi, vẫn thành công
        var r = _gfx.Apply(_game, new() { ["grass.DensityScale"] = "0" });
        Assert.True(r.Success, r.Error);
        Assert.Contains("grass.DensityScale=0", File.ReadAllText(_iniPath));

        // Mở khóa
        Assert.True(_gfx.SetReadOnly(_game, false).Success);
        Assert.False(_gfx.IsReadOnly(_game));
    }

    public void Dispose()
    {
        _log.Dispose();
        try { if (Directory.Exists(_work)) Directory.Delete(_work, true); } catch { }
    }
}
