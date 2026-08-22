using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public partial class FontViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IFontService _fonts;
    private readonly IFontPreviewService _preview;

    [ObservableProperty] private string _message = "Chọn gói font (.vhwpack) để áp dụng.";
    [ObservableProperty] private string _currentFont = "Mặc định";
    [ObservableProperty] private bool _busy;
    [ObservableProperty] private string _sampleText = "Tiếng Việt Wuthering Waves";
    [ObservableProperty] private ImageSource? _previewImage;
    [ObservableProperty] private string _previewMessage = "Chọn file font (.ttf/.otf/.ttc) để xem trước.";

    private readonly List<FontLibraryItem> _allFonts = new();
    public ObservableCollection<FontLibraryItem> Library { get; } = new();
    [ObservableProperty] private FontLibraryItem? _selectedLibraryFont;
    [ObservableProperty] private string _libraryMessage = "";
    [ObservableProperty] private string _searchText = "";

    private static string FontDir => Path.Combine(AppContext.BaseDirectory, "Fonts");

    public FontViewModel(ISettingsService settings, IFontService fonts, IFontPreviewService preview)
    {
        _settings = settings; _fonts = fonts; _preview = preview;
        LoadLibrary();
    }

    private void LoadLibrary()
    {
        _allFonts.Clear();
        Library.Clear();
        var catalog = Path.Combine(FontDir, "fonts.json");
        if (!File.Exists(catalog))
        {
            LibraryMessage = "Chưa có thư viện font (thiếu thư mục Fonts\\). Tải thêm font để chọn.";
            return;
        }
        try
        {
            using var fs = File.OpenRead(catalog);
            var doc = System.Text.Json.JsonDocument.Parse(fs);
            foreach (var e in doc.RootElement.GetProperty("fonts").EnumerateArray())
            {
                var item = new FontLibraryItem(
                    e.GetProperty("name").GetString() ?? "",
                    e.TryGetProperty("pak", out var p) ? p.GetString() ?? "" : "",
                    e.TryGetProperty("src", out var s) ? s.GetString() ?? "" : "",
                    e.TryGetProperty("sizeKb", out var k) ? k.GetDouble() : 0);
                _allFonts.Add(item);
                Library.Add(item);
            }
            LibraryMessage = $"Thư viện có {Library.Count} font tiếng Việt sẵn sàng áp dụng.";
        }
        catch (Exception ex) { LibraryMessage = "Lỗi đọc thư viện font: " + ex.Message; }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var term = SearchText.Trim().ToLowerInvariant();
        Library.Clear();
        foreach (var font in _allFonts)
        {
            if (string.IsNullOrWhiteSpace(term) || font.Name.ToLowerInvariant().Contains(term))
            {
                Library.Add(font);
            }
        }
        if (Library.Count > 0 && (SelectedLibraryFont == null || !Library.Contains(SelectedLibraryFont)))
        {
            SelectedLibraryFont = Library[0];
        }
        LibraryMessage = string.IsNullOrWhiteSpace(term)
            ? $"Thư viện có {_allFonts.Count} font tiếng Việt."
            : $"Tìm thấy {Library.Count} / {_allFonts.Count} font phù hợp với '{term}'.";
    }

    public void OnActivated()
    {
        var path = _settings.Settings.GamePath;
        var pak = string.IsNullOrWhiteSpace(path) ? null : _fonts.CurrentFontPak(path);
        CurrentFont = pak ?? "Mặc định";
    }

    [RelayCommand]
    private async Task ApplyAsync()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { Message = "Chưa chọn thư mục game."; return; }
        var dlg = new OpenFileDialog { Title = "Chọn font", Filter = "Gói VHWuWa (*.vhwpack)|*.vhwpack" };
        if (dlg.ShowDialog() != true) return;
        Busy = true;
        try
        {
            var r = await _fonts.ApplyFontAsync(path, dlg.FileName);
            Message = r.Success ? "Đã áp dụng font." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; OnActivated(); }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        Busy = true;
        try
        {
            var r = await _fonts.RestoreDefaultAsync(_settings.Settings.GamePath);
            Message = r.Success ? "Đã khôi phục font mặc định." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; OnActivated(); }
    }

    [RelayCommand]
    private void PreviewFont()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Chọn font để xem trước",
            Filter = "Font (*.ttf;*.otf;*.ttc)|*.ttf;*.otf;*.ttc|Tất cả (*.*)|*.*"
        };
        if (dlg.ShowDialog() != true) return;
        RenderPreview(dlg.FileName);
    }

    private string? _lastFontPath;

    private void RenderPreview(string fontPath)
    {
        _lastFontPath = fontPath;
        var png = _preview.RenderPreview(fontPath, SampleText);
        if (png is null)
        {
            PreviewImage = null;
            PreviewMessage = "Không đọc được font này.";
            return;
        }
        using var ms = new MemoryStream(png);
        var img = new BitmapImage();
        img.BeginInit();
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.StreamSource = ms;
        img.EndInit();
        img.Freeze();
        PreviewImage = img;
        PreviewMessage = Path.GetFileName(fontPath);
    }

    partial void OnSampleTextChanged(string value)
    {
        if (!string.IsNullOrEmpty(_lastFontPath)) RenderPreview(_lastFontPath);
    }

    [ObservableProperty] private bool _isFontDownloaded = true;
    [ObservableProperty] private bool _showDownloadButton;
    [ObservableProperty] private string _applyButtonText = "✅ Đổi sang font này";
    [ObservableProperty] private string _downloadButtonText = "⬇️ Tải font này";

    private static readonly System.Net.Http.HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(45) };

    private static async Task<bool> DownloadFontPakAsync(string pakFileName, string destinationPath)
    {
        var urls = new[]
        {
            $"https://raw.githubusercontent.com/WahuVN/Viet-Hoa-WuWa/main/Fonts/{Uri.EscapeDataString(pakFileName)}",
            $"https://github.com/WahuVN/Viet-Hoa-WuWa/releases/download/v2.0.0-fonts/{Uri.EscapeDataString(pakFileName)}"
        };

        foreach (var url in urls)
        {
            try
            {
                using var res = await _http.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead);
                if (res.IsSuccessStatusCode)
                {
                    var dir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    var tempFile = destinationPath + ".tmp";
                    using (var s = await res.Content.ReadAsStreamAsync())
                    using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await s.CopyToAsync(fs);
                    }
                    if (File.Exists(destinationPath)) File.Delete(destinationPath);
                    File.Move(tempFile, destinationPath);
                    return true;
                }
            }
            catch { }
        }
        return false;
    }

    private CancellationTokenSource? _fontDownloadCts;

    partial void OnSelectedLibraryFontChanged(FontLibraryItem? value)
    {
        if (value is null) return;
        if (!string.IsNullOrEmpty(value.Src) && File.Exists(value.Src))
            RenderPreview(value.Src);
        else
            RenderPreview(value.Name);

        var pak = Path.Combine(FontDir, value.Pak);
        IsFontDownloaded = File.Exists(pak);
        ShowDownloadButton = !IsFontDownloaded;

        if (IsFontDownloaded)
        {
            ApplyButtonText = "✅ Đổi sang font này";
            LibraryMessage = $"Font '{value.Name}' đã có sẵn trên máy.";
        }
        else
        {
            ApplyButtonText = "⏳ Đang tải & chuẩn bị font...";
            DownloadButtonText = $"⬇️ Tải font ({value.SizeKb:0} KB)";
            LibraryMessage = $"Đang tự động tải font '{value.Name}' từ GitHub về máy...";

            _fontDownloadCts?.Cancel();
            _fontDownloadCts = new CancellationTokenSource();
            var ct = _fontDownloadCts.Token;
            var currentItem = value;

            _ = Task.Run(async () =>
            {
                var ok = await DownloadFontPakAsync(currentItem.Pak, pak);
                if (ok && File.Exists(pak) && !ct.IsCancellationRequested)
                {
                    System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        if (SelectedLibraryFont == currentItem)
                        {
                            IsFontDownloaded = true;
                            ShowDownloadButton = false;
                            ApplyButtonText = "✅ Đổi sang font này";
                            LibraryMessage = $"✅ Đã tải xong font '{currentItem.Name}', sẵn sàng áp dụng!";
                        }
                    });
                }
            }, ct);
        }
    }

    [RelayCommand]
    private async Task DownloadFontOnlyAsync()
    {
        if (SelectedLibraryFont is null) return;
        var pak = Path.Combine(FontDir, SelectedLibraryFont.Pak);
        if (File.Exists(pak))
        {
            LibraryMessage = $"Font '{SelectedLibraryFont.Name}' đã có sẵn trên máy.";
            IsFontDownloaded = true;
            ShowDownloadButton = false;
            ApplyButtonText = "✅ Đổi sang font này";
            return;
        }

        Busy = true;
        LibraryMessage = $"Đang tải font '{SelectedLibraryFont.Name}' từ GitHub...";
        try
        {
            var ok = await DownloadFontPakAsync(SelectedLibraryFont.Pak, pak);
            if (ok && File.Exists(pak))
            {
                IsFontDownloaded = true;
                ShowDownloadButton = false;
                ApplyButtonText = "✅ Đổi sang font này";
                LibraryMessage = $"✅ Đã tải xong font '{SelectedLibraryFont.Name}' về máy!";
            }
            else
            {
                LibraryMessage = $"❌ Không thể tải font '{SelectedLibraryFont.Name}'. Vui lòng kiểm tra mạng.";
            }
        }
        finally
        {
            Busy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyLibraryFontAsync()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { LibraryMessage = "Chưa chọn thư mục game."; return; }
        if (SelectedLibraryFont is null) { LibraryMessage = "Hãy chọn 1 font trong danh sách."; return; }
        var pak = Path.Combine(FontDir, SelectedLibraryFont.Pak);
        Busy = true;
        try
        {
            if (!File.Exists(pak))
            {
                LibraryMessage = $"Đang tải font '{SelectedLibraryFont.Name}' từ GitHub...";
                var ok = await DownloadFontPakAsync(SelectedLibraryFont.Pak, pak);
                if (!ok || !File.Exists(pak))
                {
                    LibraryMessage = $"❌ Không thể tải file font '{SelectedLibraryFont.Pak}'. Vui lòng kiểm tra kết nối mạng.";
                    return;
                }
                IsFontDownloaded = true;
                ShowDownloadButton = false;
                ApplyButtonText = "✅ Đổi sang font này";
            }

            var r = await _fonts.ApplyFontPakAsync(path, pak);
            LibraryMessage = r.Success
                ? $"✅ Đã áp dụng font: {SelectedLibraryFont.Name}. Khởi động lại game để thấy."
                : "Lỗi: " + r.Error;
        }
        catch (Exception ex)
        {
            LibraryMessage = "Lỗi: " + ex.Message;
        }
        finally { Busy = false; OnActivated(); }
    }

    [RelayCommand]
    private async Task RemoveFontAsync()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { LibraryMessage = "Chưa chọn thư mục game."; return; }
        Busy = true;
        try
        {
            var r = await _fonts.RemoveFontPaksAsync(path);
            LibraryMessage = r.Success ? "Đã gỡ font (về font mặc định của bản VH)." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; OnActivated(); }
    }
}

/// <summary>1 mục trong thư viện font (Fonts/fonts.json).</summary>
public sealed record FontLibraryItem(string Name, string Pak, string Src, double SizeKb)
{
    public string Display => SizeKb > 0 ? $"{Name}  ·  {SizeKb:0} KB" : Name;
}
