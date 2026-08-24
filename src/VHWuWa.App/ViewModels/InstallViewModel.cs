using System.IO;
using System.Diagnostics;
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
    [ObservableProperty] private bool _hasQuarantine;
    [ObservableProperty] private string _lastQuarantinePath = "";

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
            ? $"⚠ Phát hiện {conflicts.Count} mục có thể xung đột:\n• " + string.Join("\n• ", conflicts.Take(8))
              + (conflicts.Count > 8 ? $"\n• … và {conflicts.Count - 8} mục khác" : "")
              + "\nBạn có thể cách ly để giữ bản sao hoặc chọn Xóa mod xung đột để xóa vĩnh viễn."
            : "✔ Không phát hiện mod khác có thể xung đột.";
        FontAvailable = content.FontPak is not null;
        HasHanViet = File.Exists(HanVietPakPath);
        HanVietDownloadBtnText = HasHanViet ? "✔ Đã tải gói Hán Việt" : "⬇️ Tải gói Hán Việt (61 MB)";

        if (!content.HasLoader)
        {
            ContentText = "❌ Thiếu bộ loader Việt hóa trong thư mục content\\loader. Không thể cài.";
            CanInstall = false;
        }
        else
        {
            var have = new List<string>();
            if (HasHanViet) have.Add("Hán Việt");
            if (content.HasEnglish) have.Add("Tiếng Anh");
            ContentText = "✔ Sẵn sàng cài đặt — Bản Tiếng Anh có sẵn" + (HasHanViet ? " + Bản Hán Việt đã tải" : " (Bản Hán Việt có thể tải online)");
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

    partial void OnVariantHanVietChanged(bool value)
    {
        if (value)
        {
            VariantEnglish = false;
            if (!File.Exists(HanVietPakPath) && !Busy)
            {
                _ = DownloadHanVietAsync();
            }
        }
    }
    partial void OnVariantEnglishChanged(bool value) { if (value) VariantHanViet = false; }
    partial void OnCanInstallChanged(bool value) => InstallCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void CheckConflicts() => Refresh();

    [RelayCommand]
    private void QuarantineConflicts()
    {
        var game = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(game) || !HasConflicts)
        {
            Summary = "Không có mod xung đột để cách ly.";
            return;
        }
        var answer = System.Windows.MessageBox.Show(
            "VHWuWa sẽ chuyển các PAK/loader xung đột ra khỏi thư mục game vào vùng cách ly.\n\n"
            + "Không file nào bị xóa vĩnh viễn. Bạn có muốn tiếp tục?",
            "Cách ly mod xung đột", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);
        if (answer != System.Windows.MessageBoxResult.Yes) return;

        Busy = true;
        try
        {
            var result = _viet.QuarantineConflicts(game);
            if (!result.Success || result.Value is null)
            {
                Summary = "❌ " + result.Error;
                return;
            }
            LastQuarantinePath = result.Value.QuarantineDirectory;
            HasQuarantine = true;
            Summary = $"✅ Đã cách ly {result.Value.MovedFiles.Count} file mod xung đột. "
                + "Có thể mở vùng cách ly để xem hoặc khôi phục thủ công.";
        }
        finally
        {
            Busy = false;
            Refresh();
        }
    }

    [RelayCommand]
    private void OpenQuarantine()
    {
        if (Directory.Exists(LastQuarantinePath))
            Process.Start(new ProcessStartInfo(LastQuarantinePath) { UseShellExecute = true });
    }

    [RelayCommand]
    private void DeleteConflicts()
    {
        var game = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(game) || !HasConflicts)
        {
            Summary = "Không có mod xung đột để xóa.";
            return;
        }

        var answer = System.Windows.MessageBox.Show(
            "XÓA VĨNH VIỄN toàn bộ file mod xung đột mà ứng dụng vừa liệt kê?\n\n"
            + "Thao tác này không tạo bản sao và không thể hoàn tác. Nếu muốn giữ bản sao, hãy dùng Cách ly mod xung đột.",
            "Xóa mod xung đột", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning,
            System.Windows.MessageBoxResult.No);
        if (answer != System.Windows.MessageBoxResult.Yes) return;

        Busy = true;
        try
        {
            var result = _viet.DeleteConflicts(game);
            Summary = result.Success
                ? $"✅ Đã xóa vĩnh viễn {result.Value} file mod xung đột."
                : "❌ " + result.Error;
        }
        finally
        {
            Busy = false;
            Refresh();
        }
    }

    [ObservableProperty] private bool _hasHanViet;
    [ObservableProperty] private string _hanVietDownloadBtnText = "⬇️ Tải gói Hán Việt";

    private static readonly System.Net.Http.HttpClient _http = new() { Timeout = TimeSpan.FromMinutes(5) };

    private string HanVietPakPath => Path.Combine(AppContext.BaseDirectory, "content", "WuWaVH_HanViet_99_P.pak");

    private async Task<bool> DownloadHanVietInternalAsync(IProgress<InstallProgress>? prog = null, CancellationToken ct = default)
    {
        var dst = HanVietPakPath;
        var dir = Path.GetDirectoryName(dst);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        // Luôn lấy asset của release mới nhất để bản app cũ không bị khóa vào một tag cố định.
        var url = "https://github.com/WahuVN/Viet-Hoa-WuWa/releases/latest/download/WuWaVH_HanViet_99_P.pak";
        try
        {
            using var response = await _http.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) return false;

            var totalBytes = response.Content.Headers.ContentLength ?? 61846666L;
            var tempFile = dst + ".tmp";

            using (var stream = await response.Content.ReadAsStreamAsync(ct))
            using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920];
                long totalRead = 0;
                int read;
                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read, ct);
                    totalRead += read;
                    var pct = (int)((totalRead * 100) / totalBytes);
                    prog?.Report(new InstallProgress(pct, $"Tải gói Hán Việt ({totalRead / 1048576.0:F1}/{totalBytes / 1048576.0:F1} MB)", 1, 2));
                }
            }

            if (File.Exists(dst)) File.Delete(dst);
            File.Move(tempFile, dst);
            return true;
        }
        catch { return false; }
    }

    [RelayCommand]
    private async Task DownloadHanVietAsync()
    {
        if (File.Exists(HanVietPakPath))
        {
            Summary = "Gói Hán Việt đã có sẵn trên máy.";
            Refresh();
            return;
        }

        _cts = new CancellationTokenSource();
        Busy = true; Progress = 0; ProgressText = "Đang kết nối tải gói Hán Việt...";
        Summary = "Đang tải bản dịch Hán Việt từ GitHub...";
        var progress = new Progress<InstallProgress>(p =>
        {
            Progress = p.Percent;
            ProgressText = $"{p.CurrentFile} ({p.Percent}%)";
        });

        try
        {
            var ok = await DownloadHanVietInternalAsync(progress, _cts.Token);
            Summary = ok ? "✅ Đã tải xong gói Hán Việt! Bạn có thể bấm 'Cài Việt hóa' ngay." : "❌ Lỗi: Không thể tải từ GitHub. Vui lòng thử lại.";
        }
        finally
        {
            Busy = false; _cts?.Dispose(); _cts = null; Refresh();
        }
    }

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
            if (variant == NameVariant.HanViet && !File.Exists(HanVietPakPath))
            {
                Summary = "Đang tự động tải gói Hán Việt từ GitHub trước khi cài...";
                var ok = await DownloadHanVietInternalAsync(progress, _cts.Token);
                if (!ok || !File.Exists(HanVietPakPath))
                {
                    Summary = "❌ Không thể tải gói Hán Việt từ GitHub. Hãy kiểm tra kết nối mạng hoặc thử lại.";
                    return;
                }
            }

            var r = await _viet.InstallAsync(game, variant, ApplyFont && FontAvailable, progress, _cts.Token);
            Summary = r.Success
                ? "✅ Cài xong! Vào game đặt Text Language = English. Có thể dùng DirectX 11 hoặc 12."
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
            if (!r.Success && r.Error?.Contains("xác nhận Xóa luôn", StringComparison.OrdinalIgnoreCase) == true)
            {
                var answer = System.Windows.MessageBox.Show(
                    r.Error + "\n\nBạn có chắc muốn xóa các file này và tiếp tục gỡ Việt hóa?",
                    "Gỡ và xóa file đã thay đổi", System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning,
                    System.Windows.MessageBoxResult.No);
                if (answer == System.Windows.MessageBoxResult.Yes)
                    r = await _viet.UninstallAsync(game, forceRemoveChangedFiles: true);
            }
            Summary = r.Success ? "✅ Đã gỡ Việt hóa và khôi phục file gốc." : "❌ Lỗi: " + r.Error;
        }
        finally { Busy = false; Refresh(); }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();
}
