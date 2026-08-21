using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;
using VHWuWa.Core.Abstractions;

namespace VHWuWa.Infrastructure;

/// <summary>
/// Render ảnh xem trước font bằng System.Drawing (chỉ chạy trên Windows).
/// Không cài font vào hệ thống — dùng PrivateFontCollection trong bộ nhớ.
/// </summary>
public sealed class FontPreviewService : IFontPreviewService
{
    public byte[]? RenderPreview(string fontFilePath, string sampleText, int fontSize = 30)
    {
        if (!OperatingSystem.IsWindows()) return null;
        if (string.IsNullOrWhiteSpace(fontFilePath) || !File.Exists(fontFilePath)) return null;
        if (string.IsNullOrWhiteSpace(sampleText)) sampleText = "Tiếng Việt";

        try
        {
            return RenderWindows(fontFilePath, sampleText, Math.Clamp(fontSize, 8, 96));
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static byte[] RenderWindows(string fontFilePath, string sampleText, int fontSize)
    {
        using var pfc = new PrivateFontCollection();
        pfc.AddFontFile(fontFilePath);
        var family = pfc.Families[0];

        const int width = 1040;
        const int height = 300;
        using var bmp = new Bitmap(width, height);
        bmp.SetResolution(120, 120);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.FromArgb(28, 30, 38));      // nền tối dịu, tương phản cao với chữ trắng
        g.SmoothingMode = SmoothingMode.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        int big = Math.Clamp(fontSize + 8, 24, 96);
        using var title = new Font(family, big, FontStyle.Bold, GraphicsUnit.Pixel);
        using var body = new Font(family, big * 0.62f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var small = new Font(family, big * 0.42f, FontStyle.Regular, GraphicsUnit.Pixel);
        using var white = new SolidBrush(Color.White);
        using var soft = new SolidBrush(Color.FromArgb(210, 214, 224));
        using var accent = new SolidBrush(Color.FromArgb(150, 135, 255)); // tím sáng

        g.DrawString(family.Name, small, accent, new PointF(20, 14));
        g.DrawString(sampleText, title, white, new RectangleF(20, 46, width - 40, big + 20));
        g.DrawString("Tiếng Việt đủ dấu: ăâđêôơư — À Á Ả Ã Ạ · Ệ Ỡ Ự · ýỳỷỹỵ",
            body, soft, new RectangleF(20, 46 + big + 26, width - 40, big));
        g.DrawString("0123456789  ·  Wuthering Waves  ·  Kim Tịch / Jinhsi",
            body, soft, new RectangleF(20, 46 + big + 26 + (int)(big * 0.9f), width - 40, big));

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }
}
