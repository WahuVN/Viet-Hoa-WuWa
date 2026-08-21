using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public partial class InstallViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IGameDetectionService _detect;
    private readonly IViethoaInstaller _viet;
    private CancellationTokenSource? _cts;

    // Biến thể tên nhân vật: true = Hán Việt (mặc định), false = Tiếng Anh
    [ObservableProperty] private bool _variantHanViet = true;
    [ObservableProperty] private bool _variantEnglish;
    [ObservableProperty] private bool _applyFont = true;

    [ObservableProperty] private string _gamePathText = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _contentText = "";
    [ObservableProperty] private string _summary = "Chọn kiểu tên nhân vật rồi bấm Cài Việt hóa.";
    [ObservableProperty] private int _progress;
    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private bool _busy;
    [ObservableProperty] private bool _canInstall;
    [ObservableProperty] private bool _fontAvailable;
    [ObservableProperty] private bool _hasConflicts;
    [ObservableProperty] private string _conflictText = "✔ Không phát hiện mod khác.";

    public InstallViewModel(ISettingsService settings, IGameDetectionService detect, IViethoaInstaller viet)
    {
        _settings = settings; _detect = detect; _viet = viet;
    }

    public void OnActivated() => Refresh();

    private void Refresh()
    {
        var game = _settings.Settings.GamePath;
        var valid = !string.IsNullOrWhiteSpace(game) && _detect.Validate(game).IsValid;
        GamePathText = string.IsNullOrWhiteSpace(game)
            ? "⚠ Chưa chọn thư mục game (vào Trang chủ để chọn / tự dò)."
            : (valid ? "🎮 Game: " + game : "⚠ Đường dẫn game chưa hợp lệ: " + game);

        var content = _viet.InspectContent();
        var conflicts = valid ? _viet.FindConflicts(game) : Array.Empty<string>();
        HasConflicts = conflicts.Count > 0;
        ConflictText = HasConflicts
            ? "⚠ Phát hiện mod có thể xung đột:\n• " + string.Join("\n• ", conflicts.Take(6))
              + "\nHãy gỡ hoặc tắt các mod trên trước khi cài Việt hóa."
            : "✔ Không phát hiện mod khác có thể xung đột.";
        FontAvailable = content.FontPak is not null;
        if (!content.Ready)
        {
            ContentText = "❌ Thiếu nội dung Việt hóa đi kèm (thư mục content\\). Không thể cài.";
            CanInstall = false;
        }
        else
        {
            var have = new List<string>();
            if (content.HasHanViet) have.Add("Hán Việt");
            if (content.HasEnglish) have.Add("Tiếng Anh");
            ContentText = "✔ Nội dung sẵn sàng — pak: " + string.Join(" + ", have)
                + (content.FontPak is not null ? " · có font kèm" : "")
                + " · mỗi lựa chọn dùng PAK riêng · 85 DB dịch hữu dụng đã kiểm chứng";
            CanInstall = valid && !HasConflicts;
        }

        if (valid)
        {
            var st = _viet.GetStatus(game);
            StatusText = st.Installed
                ? $"● Đã cài Việt hóa ({st.VariantLabel}){(st.FontPak is not null ? " · font: " + st.FontPak : "")}"
                : "○ Chưa cài Việt hóa.";
        }
        else StatusText = "";
    }

    partial void OnVariantHanVietChanged(bool value) { if (value) VariantEnglish = false; }
    partial void OnVariantEnglishChanged(bool value) { if (value) VariantHanViet = false; }
    partial void OnCanInstallChanged(bool value) => InstallCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void CheckConflicts() => Refresh();

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        var game = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(game)) { Summary = "Chưa chọn thư mục game (ở Trang chủ)."; return; }
        var variant = VariantEnglish ? NameVariant.English : NameVariant.HanViet;
        _cts = new CancellationTokenSource();
        Busy = true; Progress = 0; ProgressText = "Bắt đầu...";
        Summary = "⚠ HÃY TẮT GAME trước khi cài.";
        var progress = new Progress<InstallProgress>(p =>
        {
            Progress = p.Percent;
            ProgressText = $"{p.Completed}/{p.Total} — {p.CurrentFile}";
        });
        try
        {
            var r = await _viet.InstallAsync(game, variant, ApplyFont && FontAvailable, progress, _cts.Token);
            Summary = r.Success
                ? "✅ Cài xong và đã hậu kiểm đủ PAK, SIG, font, loader, marker bằng dung lượng + SHA-256. Vào game đặt Text Language = English."
                : "❌ Lỗi: " + r.Error;
        }
        finally { Busy = false; _cts?.Dispose(); _cts = null; Refresh(); }
    }

    [RelayCommand]
    private async Task UninstallAsync()
    {
        var game = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(game)) { Summary = "Chưa chọn thư mục game."; return; }
        Busy = true; Summary = "Đang gỡ Việt hóa...";
        try
        {
            var r = await _viet.UninstallAsync(game);
            Summary = r.Success ? "✅ Đã gỡ Việt hóa và khôi phục file gốc." : "❌ Lỗi: " + r.Error;
        }
        finally { Busy = false; Refresh(); }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();
}
