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
    public byte[]? RenderPreview(string fontPathOrFamily, string sampleText, int fontSize = 30)
    {
        if (!OperatingSystem.IsWindows()) return null;
        if (string.IsNullOrWhiteSpace(fontPathOrFamily)) return null;

        var isFilePath = fontPathOrFamily.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                         fontPathOrFamily.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
                         fontPathOrFamily.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase) ||
                         fontPathOrFamily.Contains('\\') || fontPathOrFamily.Contains('/');

        if (isFilePath && !File.Exists(fontPathOrFamily))
            return null;

        if (string.IsNullOrWhiteSpace(sampleText)) sampleText = "Tiếng Việt Wuthering Waves";

        try
        {
            return RenderWindows(fontPathOrFamily, sampleText, Math.Clamp(fontSize, 8, 96));
        }
        catch
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static byte[] RenderWindows(string fontPathOrFamily, string sampleText, int fontSize)
    {
        FontFamily? family = null;
        PrivateFontCollection? pfc = null;

        try
        {
            if (File.Exists(fontPathOrFamily))
            {
                try
                {
                    pfc = new PrivateFontCollection();
                    pfc.AddFontFile(fontPathOrFamily);
                    if (pfc.Families.Length > 0)
                        family = pfc.Families[0];
                }
                catch { }
            }

            if (family is null)
            {
                try { family = new FontFamily(fontPathOrFamily); }
                catch { family = FontFamily.GenericSansSerif; }
            }

            int big = Math.Clamp(fontSize + 6, 20, 96);
            var styleTitle = family.IsStyleAvailable(FontStyle.Bold) ? FontStyle.Bold : FontStyle.Regular;
            var styleBody = family.IsStyleAvailable(FontStyle.Regular) ? FontStyle.Regular : FontStyle.Bold;

            using var title = new Font(family, big, styleTitle, GraphicsUnit.Pixel);
            using var body = new Font(family, big * 0.56f, styleBody, GraphicsUnit.Pixel);
            using var small = new Font(family, Math.Max(14, big * 0.35f), styleBody, GraphicsUnit.Pixel);

            const string line2 = "Tiếng Việt đủ dấu: ăâđêôơư — À Á Ả Ã Ạ · Ệ Ỡ Ự · ýỳỷỹỵ";
            const string line3 = "0123456789  ·  Wuthering Waves  ·  Kim Tịch / Jinhsi";

            // Đo kích thước thực tế của từng dòng bằng GenericTypographic để không bị cắt xén
            using var sf = new StringFormat(StringFormat.GenericTypographic)
            {
                FormatFlags = StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap
            };

            using var measureBmp = new Bitmap(1, 1);
            using var measureG = Graphics.FromImage(measureBmp);
            measureG.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var sizeSmall = measureG.MeasureString(family.Name, small, PointF.Empty, sf);
            var sizeTitle = measureG.MeasureString(sampleText, title, PointF.Empty, sf);
            var sizeBody1 = measureG.MeasureString(line2, body, PointF.Empty, sf);
            var sizeBody2 = measureG.MeasureString(line3, body, PointF.Empty, sf);

            float maxTextWidth = Math.Max(Math.Max(sizeTitle.Width, sizeBody1.Width), Math.Max(sizeBody2.Width, sizeSmall.Width));

            // Đảm bảo chiều rộng đủ lớn cho font viết tay/uốn lượn và không bao giờ bị cắt mép phải
            int paddingX = 28;
            int width = Math.Max(1400, (int)Math.Ceiling(maxTextWidth) + paddingX * 2 + 50);

            // Khoảng cách dòng thoáng đãng, tính thêm không gian cho dấu thanh và đuôi chữ (g, y, p, q, j)
            float extraLinePadding = Math.Max(18f, big * 0.32f);
            float y0 = 22f;
            float y1 = y0 + sizeSmall.Height + 14f;
            float y2 = y1 + sizeTitle.Height + extraLinePadding;
            float y3 = y2 + sizeBody1.Height + extraLinePadding;
            int height = (int)Math.Ceiling(y3 + sizeBody2.Height + 40f);

            using var bmp = new Bitmap(width, height);
            bmp.SetResolution(120, 120);
            using var g = Graphics.FromImage(bmp);
            // Nền xám hiện đại (Slate Gray #282A36), tương phản xuất sắc giúp nét chữ rõ ràng và nổi bật
            g.Clear(Color.FromArgb(40, 42, 54));
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using var white = new SolidBrush(Color.FromArgb(255, 255, 255));
            using var soft = new SolidBrush(Color.FromArgb(226, 232, 240));
            using var accent = new SolidBrush(Color.FromArgb(167, 155, 255)); // tím sáng

            // Vẽ trực tiếp theo toạ độ PointF không dùng bounding box hạn chế -> KHÔNG BAO GIỜ BỊ CẮT CHỮ
            g.DrawString(family.Name, small, accent, new PointF(paddingX, y0), sf);
            g.DrawString(sampleText, title, white, new PointF(paddingX, y1), sf);
            g.DrawString(line2, body, soft, new PointF(paddingX, y2), sf);
            g.DrawString(line3, body, soft, new PointF(paddingX, y3), sf);

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
        finally
        {
            pfc?.Dispose();
        }
    }
}
