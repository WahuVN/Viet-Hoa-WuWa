using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.App.Services;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public partial class FontViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IFontService _fonts;
    private readonly IFontPreviewService _preview;

    [ObservableProperty] private string _message = "Sẵn sàng.";
    [ObservableProperty] private string _currentFont = "Mặc định";
    [ObservableProperty] private bool _busy;
    [ObservableProperty] private string _sampleText = "Người Lữ Hành đã thức tỉnh sức mạnh. Phần thưởng quý giá sẽ được trao sau trận chiến. ắ ằ ẳ ẵ ặ · ấ ầ ẩ ẫ ậ · ữ ự ỡ ợ";
    [ObservableProperty] private ImageSource? _previewImage;
    [ObservableProperty] private string _previewMessage = "Chọn một font để xem trước.";
    [ObservableProperty] private int _previewFontSize = 30;
    [ObservableProperty] private int _fontWeight = 650;
    [ObservableProperty] private int _minimumFontWeight = 100;
    [ObservableProperty] private int _maximumFontWeight = 900;
    [ObservableProperty] private bool _canAdjustFontWeight;
    [ObservableProperty] private string _weightHelp = "Font tĩnh: hãy chọn bản Regular / SemiBold / Bold trong thư viện.";
    public string WeightLabel => $"Weight {FontWeight}";
    public string PreviewFontSizeLabel => $"{PreviewFontSize} px";

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
            LibraryMessage = "Không tìm thấy thư viện font.";
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
            SelectedLibraryFont = Library.FirstOrDefault(f => f.Name == "Lexend Variable") ?? Library.FirstOrDefault();
            LibraryMessage = $"{Library.Count} font có sẵn";
        }
        catch (Exception ex) { LibraryMessage = "Không đọc được thư viện: " + VHWuWa.Core.Services.ErrorFormatter.Humanize(null, ex); }
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
            ? $"{_allFonts.Count} font có sẵn"
            : $"{Library.Count}/{_allFonts.Count} kết quả";
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
            Message = r.Success ? "✅ Đã áp dụng gói font." : "❌ " + r.Error;
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
            Message = r.Success ? "✅ Đã khôi phục font mặc định." : "❌ " + r.Error;
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
        RenderPreview(dlg.FileName, isCustom: true);
    }

    private string? _lastFontPath;
    [ObservableProperty] private bool _hasCustomFontSelected;
    private CancellationTokenSource? _previewRefreshCts;

    private static string ResolveSource(string src)
        => Path.IsPathRooted(src) ? src : Path.Combine(FontDir, src);

    private void RenderPreview(string fontPath, bool isCustom = false)
    {
        _lastFontPath = fontPath;
        HasCustomFontSelected = isCustom && File.Exists(fontPath);
        var info = FontWeightTools.Inspect(fontPath);
        CanAdjustFontWeight = info.IsVariable;
        MinimumFontWeight = info.IsVariable ? info.Minimum : info.Default;
        MaximumFontWeight = info.IsVariable ? info.Maximum : info.Default;
        WeightHelp = info.IsVariable
            ? $"Variable Font: chỉnh được từ {info.Minimum} đến {info.Maximum}; xuất font thật trước khi cài."
            : $"Font tĩnh (weight {info.Default}): chọn phiên bản đậm khác trong thư viện.";
        FontWeight = info.IsVariable ? Math.Clamp(650, info.Minimum, info.Maximum) : info.Default;
        RequestPreviewRefresh();
    }

    private void DisplayPreview(string path)
    {
        var png = _preview.RenderPreview(path, SampleText, PreviewFontSize);
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
        var displayName = Path.GetFileName(_lastFontPath) ?? "Font";
        PreviewMessage = CanAdjustFontWeight ? $"Weight {FontWeight} · {displayName}" : displayName;
    }

    private void RequestPreviewRefresh()
    {
        if (string.IsNullOrWhiteSpace(_lastFontPath)) return;
        _previewRefreshCts?.Cancel();
        _previewRefreshCts = new CancellationTokenSource();
        _ = RefreshPreviewAfterDelayAsync(_previewRefreshCts.Token);
    }

    partial void OnFontWeightChanged(int value)
    {
        OnPropertyChanged(nameof(WeightLabel));
        if (CanAdjustFontWeight) RequestPreviewRefresh();
    }

    partial void OnPreviewFontSizeChanged(int value)
    {
        OnPropertyChanged(nameof(PreviewFontSizeLabel));
        RequestPreviewRefresh();
    }

    private async Task RefreshPreviewAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(180, token);
            var source = _lastFontPath;
            if (string.IsNullOrWhiteSpace(source)) return;
            var actual = CanAdjustFontWeight
                ? await FontWeightTools.InstantiateAsync(source, FontWeight, token)
                : source;
            if (!token.IsCancellationRequested && source == _lastFontPath) DisplayPreview(actual);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!token.IsCancellationRequested)
            {
                PreviewImage = null;
                PreviewMessage = "Không tạo được bản xem trước weight: " +
                    VHWuWa.Core.Services.ErrorFormatter.Humanize(null, ex);
            }
        }
    }

    [RelayCommand]
    private void IncreasePreviewSize() => PreviewFontSize = Math.Min(72, PreviewFontSize + 2);

    [RelayCommand]
    private void DecreasePreviewSize() => PreviewFontSize = Math.Max(12, PreviewFontSize - 2);

    [RelayCommand]
    private void ResetPreviewSize() => PreviewFontSize = 30;

    [RelayCommand]
    private async Task ApplyCustomFontAsync()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { Message = "Chưa chọn thư mục game."; return; }
        if (string.IsNullOrWhiteSpace(_lastFontPath) || !File.Exists(_lastFontPath))
        {
            Message = "Hãy bấm 'Chọn font ngoài' để chọn file font (.ttf/.otf) trước.";
            return;
        }

        Busy = true;
        Message = $"Đang tạo và cài {Path.GetFileName(_lastFontPath)}…";
        try
        {
            var selectedPath = _lastFontPath!;
            var effectiveFont = CanAdjustFontWeight
                ? await FontWeightTools.InstantiateAsync(selectedPath, FontWeight)
                : selectedPath;
            var r = await _fonts.BuildAndApplyCustomFontAsync(path, effectiveFont);
            Message = r.Success ? $"✅ Đã cài {Path.GetFileName(_lastFontPath)} · weight {FontWeight}." : "❌ " + r.Error;
        }
        catch (Exception ex)
        {
            Message = "❌ " + VHWuWa.Core.Services.ErrorFormatter.Humanize(null, ex);
        }
        finally
        {
            Busy = false;
            OnActivated();
        }
    }

    partial void OnSampleTextChanged(string value)
    {
        RequestPreviewRefresh();
    }

    [ObservableProperty] private bool _isFontDownloaded = true;
    [ObservableProperty] private bool _showDownloadButton;
    [ObservableProperty] private string _applyButtonText = "Áp dụng";
    [ObservableProperty] private string _downloadButtonText = "Tải font";

    private static readonly System.Net.Http.HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(45) };

    private static async Task<bool> DownloadFontPakAsync(string pakFileName, string destinationPath)
    {
        var urls = new[]
        {
            $"https://raw.githubusercontent.com/WahuVN/Viet-Hoa-WuWa/main/Fonts/{Uri.EscapeDataString(pakFileName)}"
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

    partial void OnSelectedLibraryFontChanged(FontLibraryItem? value)
    {
        if (value is null) return;
        var source = ResolveSource(value.Src);
        if (!string.IsNullOrWhiteSpace(value.Src) && File.Exists(source))
            RenderPreview(source);
        else
            RenderPreview(value.Name);

        if (CanAdjustFontWeight)
        {
            IsFontDownloaded = true;
            ShowDownloadButton = false;
            ApplyButtonText = "Tạo & áp dụng";
            LibraryMessage = $"Font biến thiên · {value.Name}";
            return;
        }

        var pak = Path.Combine(FontDir, value.Pak);
        IsFontDownloaded = File.Exists(pak);
        ShowDownloadButton = !IsFontDownloaded;

        if (IsFontDownloaded)
        {
            ApplyButtonText = "Áp dụng";
            LibraryMessage = $"Sẵn sàng · {value.Name}";
        }
        else
        {
            ApplyButtonText = "Tải và áp dụng";
            DownloadButtonText = $"Tải · {value.SizeKb:0} KB";
            LibraryMessage = $"Chưa tải · {value.Name}";
        }
    }

    [RelayCommand]
    private async Task DownloadFontOnlyAsync()
    {
        if (SelectedLibraryFont is null) return;
        if (CanAdjustFontWeight) return; // Variable fonts are generated locally.
        var pak = Path.Combine(FontDir, SelectedLibraryFont.Pak);
        if (File.Exists(pak))
        {
            LibraryMessage = $"Sẵn sàng · {SelectedLibraryFont.Name}";
            IsFontDownloaded = true;
            ShowDownloadButton = false;
            ApplyButtonText = "Áp dụng";
            return;
        }

        Busy = true;
        LibraryMessage = $"Đang tải {SelectedLibraryFont.Name}…";
        try
        {
            var ok = await DownloadFontPakAsync(SelectedLibraryFont.Pak, pak);
            if (ok && File.Exists(pak))
            {
                IsFontDownloaded = true;
                ShowDownloadButton = false;
                ApplyButtonText = "Áp dụng";
                LibraryMessage = $"✅ Đã tải {SelectedLibraryFont.Name}.";
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
            if (string.IsNullOrWhiteSpace(SelectedLibraryFont.Pak) && !CanAdjustFontWeight)
            {
                LibraryMessage = "❌ Không tìm thấy font Variable nguồn; kiểm tra Fonts/Lexend-Variable.ttf.";
                return;
            }
            if (CanAdjustFontWeight)
            {
                var source = ResolveSource(SelectedLibraryFont.Src);
                var generated = await FontWeightTools.InstantiateAsync(source, FontWeight);
                var result = await _fonts.BuildAndApplyCustomFontAsync(path, generated);
                LibraryMessage = result.Success
                    ? $"✅ Đã áp dụng {SelectedLibraryFont.Name} · weight {FontWeight}."
                    : "❌ " + result.Error;
                return;
            }
            if (!File.Exists(pak))
            {
                LibraryMessage = $"Đang tải {SelectedLibraryFont.Name}…";
                var ok = await DownloadFontPakAsync(SelectedLibraryFont.Pak, pak);
                if (!ok || !File.Exists(pak))
                {
                    LibraryMessage = $"❌ Không thể tải file font '{SelectedLibraryFont.Pak}'. Vui lòng kiểm tra kết nối mạng.";
                    return;
                }
                IsFontDownloaded = true;
                ShowDownloadButton = false;
                ApplyButtonText = "Áp dụng";
            }

            var r = await _fonts.ApplyFontPakAsync(path, pak);
            LibraryMessage = r.Success
                ? $"✅ Đã áp dụng {SelectedLibraryFont.Name}."
                : (r.Error?.StartsWith("❌") == true ? r.Error : "❌ " + r.Error);
        }
        catch (Exception ex)
        {
            LibraryMessage = "❌ " + VHWuWa.Core.Services.ErrorFormatter.Humanize(null, ex);
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
            LibraryMessage = r.Success ? "✅ Đã gỡ font tùy chỉnh." : (r.Error?.StartsWith("❌") == true ? r.Error : "❌ " + r.Error);
        }
        catch (Exception ex)
        {
            LibraryMessage = "❌ " + VHWuWa.Core.Services.ErrorFormatter.Humanize(null, ex);
        }
        finally { Busy = false; OnActivated(); }
    }
}

/// <summary>1 mục trong thư viện font (Fonts/fonts.json).</summary>
public sealed record FontLibraryItem(string Name, string Pak, string Src, double SizeKb)
{
    public string Display => SizeKb > 0 ? $"{Name}  ·  {SizeKb:0} KB" : Name;
}
