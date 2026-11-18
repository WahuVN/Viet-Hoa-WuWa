using System.IO;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;
using VHWuWa.Infrastructure;

namespace VHWuWa.App.ViewModels;

public partial class InstallViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IGameDetectionService _detect;
    private readonly IViethoaInstaller _viet;
    private readonly IUpdateService _update;
    private string _lastHanVietDownloadError = "";
    private CancellationTokenSource? _cts;
    private bool _r29Mode;
    private bool _r29InstallReady;
    private bool _r29Installed;

    // Gói nhẹ mặc định dùng bản Việt hóa giữ tên quốc tế. Hán Việt tải khi cần.
    [ObservableProperty] private bool _variantHanViet;
    [ObservableProperty] private bool _variantEnglish = true;
    [ObservableProperty] private bool _applyFont = true;
    // R29 installs text plus an independent verified Lexend 660 font mount.
    [ObservableProperty] private bool _usesNativeFont;

    [ObservableProperty] private string _gamePathText = "";
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _contentText = "";
    [ObservableProperty] private string _summary = "Chọn kiểu tên nhân vật rồi bấm Cài Việt hóa.";
    [ObservableProperty] private string _installButtonText = "Cài Việt hóa";
    [ObservableProperty] private int _progress;
    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private bool _busy;
    [ObservableProperty] private bool _canInstall;
    [ObservableProperty] private bool _fontAvailable;
    [ObservableProperty] private bool _isHanVietAvailable = true;
    [ObservableProperty] private string _optionHelpText = "";
    [ObservableProperty] private bool _hasConflicts;
    [ObservableProperty] private string _conflictText = "✔ Không phát hiện mod khác.";
    [ObservableProperty] private bool _hasQuarantine;
    [ObservableProperty] private string _lastQuarantinePath = "";
    [ObservableProperty] private bool _hasOldTranslationConflicts;
    [ObservableProperty] private string _oldTranslationConflictText = "Chưa quét dấu vết Việt hóa cũ.";

    public InstallViewModel(ISettingsService settings, IGameDetectionService detect, IViethoaInstaller viet,
        IUpdateService update)
    {
        _settings = settings; _detect = detect; _viet = viet; _update = update;
    }

    public event EventHandler? InstallationStateChanged;

    public void OnActivated() => Refresh();

    private void Refresh()
    {
        var game = _settings.Settings.GamePath;
        var valid = !string.IsNullOrWhiteSpace(game) && _detect.Validate(game).IsValid;
        GamePathText = string.IsNullOrWhiteSpace(game)
            ? "⚠ Chưa chọn thư mục game (vào Trang chủ để chọn / tự dò)."
            : (valid ? "🎮 Game: " + game : "⚠ Đường dẫn game chưa hợp lệ: " + game);

        var content = _viet.InspectContent();
        var r29Present = R29MountedPak.ReleaseRequired(AppContext.BaseDirectory)
            || R29MountedPak.PackagePresent(AppContext.BaseDirectory);
        var installStatus = valid ? _viet.GetStatus(game) : null;
        var hasLegacyFiles = r29Present && installStatus?.LegacyFilesPresent == true;
        UsesNativeFont = false;
        var conflicts = valid ? _viet.FindConflicts(game) : Array.Empty<string>();
        HasConflicts = conflicts.Count > 0;
        var oldTranslationFiles = valid && !r29Present ? _viet.FindOldTranslationConflicts(game) : Array.Empty<string>();
        var oldScanError = oldTranslationFiles.FirstOrDefault(x =>
            x.StartsWith("Không thể kiểm tra mod:", StringComparison.OrdinalIgnoreCase));
        HasOldTranslationConflicts = oldScanError is null && oldTranslationFiles.Count > 0;
        OldTranslationConflictText = oldScanError is not null ? "❌ " + oldScanError
            : HasOldTranslationConflicts
                ? $"Phát hiện {oldTranslationFiles.Count} file từ bộ Việt hóa cũ có thể dọn riêng:\n• "
                  + string.Join("\n• ", oldTranslationFiles.Take(8))
                : "✔ Không tìm thấy file Việt hóa Đặng Dev/fork có thể dọn tự động.";
        var latestQuarantine = !string.IsNullOrWhiteSpace(_viet.LastQuarantineDirectory)
            ? _viet.LastQuarantineDirectory : (valid ? _viet.FindLatestQuarantine(game) : null);
        LastQuarantinePath = latestQuarantine ?? "";
        HasQuarantine = !string.IsNullOrWhiteSpace(LastQuarantinePath)
            && Directory.Exists(LastQuarantinePath);
        ConflictText = HasConflicts
            ? $"{conflicts.Count} file mod khác:\n• " + string.Join("\n• ", conflicts.Take(8))
            : "";
        FontAvailable = content.FontPak is not null;
        HasHanViet = File.Exists(HanVietPakPath);
        var staged = StagedPakInspector.Inspect(AppContext.BaseDirectory);
        // R29 is mounted without the old DLL/font PAK; never claim that the
        // separate font option is installed when the R29 branch ignores it.
        // Only expose installable variants. R29 ships names in English only.
        IsHanVietAvailable = !r29Present;
        if (r29Present)
        {
            // R29 does not silently touch mods: show the suspected files and
            // explicitly ask before any optional quarantine.
            if (HasConflicts)
                ConflictText = "Phát hiện mod khác. Khi cài, ứng dụng sẽ hỏi trước "
                    + "khi sao lưu và tạm vô hiệu hóa:\n• "
                    + string.Join("\n• ", conflicts.Take(8));
            VariantEnglish = true;
            ApplyFont = true; // Install contract: R29 always includes Lexend 660.
            FontAvailable = content.FontPak is not null;
            HasHanViet = false;
        }
        var r29Installed = valid && installStatus is { Installed: true, Version: R29MountedPak.Release };
        _r29Mode = r29Present;
        _r29InstallReady = valid && !hasLegacyFiles;
        _r29Installed = r29Installed;
        if (!r29Present) OptionHelpText = "";
        InstallButtonText = !valid ? "Chưa có game PC"
            : hasLegacyFiles ? "Gỡ bản cũ trước"
            : r29Present ? (r29Installed ? "Cài lại Việt hóa" : "Cài Việt hóa")
            : staged.Present ? "PAK R28 chưa tương thích" : "Cài Việt hóa";
        ShowHanVietDownload = !HasHanViet && !staged.Present && !r29Present;
        HanVietDownloadBtnText = HasHanViet ? "Đã có bản Hán Việt" : "Tải bản Hán Việt (~60 MB)";

        if (r29Present)
        {
            ContentText = hasLegacyFiles
                ? "Phát hiện PAK/loader Việt hóa cũ. Không cài R29 chồng lên bộ cũ."
                : "R29 chỉ ghi tệp Việt hóa do WAHU quản lý, không thay đổi mod khác.";
            CanInstall = valid && !hasLegacyFiles;
            Summary = hasLegacyFiles
                ? "Hãy sao lưu mod nếu có, chọn Gỡ Việt hóa rồi kiểm tra lại trước khi cài R29."
                : r29Installed ? "Đã cài Việt hóa. Bấm Cài để ghi đè lại."
                : !valid ? "Hãy chọn đúng thư mục game."
                : "Sẵn sàng cài Việt hóa R29.";
        }
        else if (staged.Present)
        {
            ContentText = staged.Message;
            CanInstall = false;
            Summary = staged.ShaVerified
                ? "PAK R28 đã vượt kiểm tra offline nhưng game PC thực tế báo Corrupt pak index. Không cài gói này vào game."
                : "Gói R28 chưa vượt kiểm tra an toàn. Không cài vào game.";
        }
        else if (!content.HasLoader)
        {
            ContentText = "Thiếu bộ cài Việt hóa tương thích. Chưa thể cài.";
            CanInstall = false;
        }
        else
        {
            ContentText = HasHanViet
                ? "Sẵn sàng · Bản Việt hóa và Hán Việt đã có trên máy."
                : "Sẵn sàng · Bản Việt hóa có sẵn; Hán Việt tải khi cần.";
            // Conflict không khóa nút Cài: InstallAsync tự dọn file ngoài đã xác định
            // trước khi chèn bản VHWuWa mới; legacy VHWuWa được bảo vệ riêng.
            CanInstall = valid;
        }

        if (r29Present) RefreshR29Options();

        if (valid)
        {
            var st = installStatus!;
            StatusText = hasLegacyFiles
                ? "⚠ Còn PAK hoặc loader Việt hóa cũ; cần gỡ trước khi cài R29."
                : st.Installed
                    ? $"● Đã cài Việt hóa ({st.VariantLabel}){(st.FontPak is not null ? " · font: " + st.FontPak : "")}"
                    : "○ Chưa cài Việt hóa.";
        }
        else StatusText = "";

        // HomePage dùng HomeViewModel riêng cho badge/trạng thái tổng quan. Báo cho
        // nó refresh ngay sau mọi lần trạng thái cài đặt được quét lại, tránh panel
        // báo "Đã cài" nhưng badge phía trên vẫn giữ "Chưa cài".
        InstallationStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshR29Options()
    {
        if (!_r29Mode) return;

        var missingHanViet = VariantHanViet;
        OptionHelpText = missingHanViet
            ? "R29 hiện dùng tên Anh. Khi cài, ứng dụng tự chuyển về tên Anh và áp font Lexend 660."
            : "Mặc định cài kèm font Lexend 660; sau đó có thể đổi font mà không ảnh hưởng mod khác.";
        CanInstall = _r29InstallReady;
        if (_r29InstallReady)
            Summary = _r29Installed ? "Đã cài R29. Có thể ghi đè lại hoặc gỡ." : "Sẵn sàng ghi đè Việt hóa.";
    }

    partial void OnVariantHanVietChanged(bool value)
    {
        if (value) VariantEnglish = false;
        RefreshR29Options();
    }

    partial void OnVariantEnglishChanged(bool value)
    {
        if (value) VariantHanViet = false;
        RefreshR29Options();
    }

    partial void OnApplyFontChanged(bool value) => RefreshR29Options();
    private bool CanStartInstall() => CanInstall && !Busy;
    private bool CanManageConflicts() => HasConflicts && !Busy;
    private bool CanRemoveOldTranslationConflicts() => HasOldTranslationConflicts && !Busy;
    partial void OnHasOldTranslationConflictsChanged(bool value) =>
        RemoveOldTranslationConflictsCommand.NotifyCanExecuteChanged();

    partial void OnCanInstallChanged(bool value) => InstallCommand.NotifyCanExecuteChanged();
    partial void OnHasConflictsChanged(bool value)
    {
        DeleteConflictsCommand.NotifyCanExecuteChanged();
        QuarantineConflictsCommand.NotifyCanExecuteChanged();
    }
    partial void OnBusyChanged(bool value)
    {
        InstallCommand.NotifyCanExecuteChanged();
        DeleteConflictsCommand.NotifyCanExecuteChanged();
        QuarantineConflictsCommand.NotifyCanExecuteChanged();
        RestoreQuarantineCommand.NotifyCanExecuteChanged();
        RemoveOldTranslationConflictsCommand.NotifyCanExecuteChanged();
        CheckConflictsCommand.NotifyCanExecuteChanged();
        UninstallCommand.NotifyCanExecuteChanged();
        DownloadHanVietCommand.NotifyCanExecuteChanged();
    }

    private bool CanRefreshConflicts() => !Busy;

    [RelayCommand(CanExecute = nameof(CanRefreshConflicts))]
    private void CheckConflicts() => Refresh();

    [RelayCommand(CanExecute = nameof(CanManageConflicts))]
    private void QuarantineConflicts()
    {
        var game = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(game) || !HasConflicts)
        {
            Summary = "Không có mod xung đột để cách ly.";
            return;
        }
        var answer = System.Windows.MessageBox.Show(
            "Danh sách cần xem trước:\n• " + string.Join("\n• ",
                _viet.FindConflicts(game).Take(15))
            + "\n\nCác file này có thể thuộc mod khác và không nhất thiết gây lỗi. "
            + "Nếu đồng ý, ứng dụng sao lưu rồi tạm vô hiệu hóa chúng.\n"
            + "Không xóa vĩnh viễn. Tiếp tục?",
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

    [RelayCommand(CanExecute = nameof(CanRemoveOldTranslationConflicts))]
    private void RemoveOldTranslationConflicts()
    {
        var game = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(game) || !_detect.Validate(game).IsValid)
        {
            Summary = "❌ Chưa chọn đúng thư mục game.";
            return;
        }

        var files = _viet.FindOldTranslationConflicts(game);
        var scanError = files.FirstOrDefault(x =>
            x.StartsWith("Không thể kiểm tra mod:", StringComparison.OrdinalIgnoreCase));
        if (scanError is not null)
        {
            Summary = "❌ " + scanError;
            return;
        }
        if (files.Count == 0)
        {
            Summary = "Không còn file Việt hóa cũ đã xác minh để dọn.";
            Refresh();
            return;
        }

        var preview = string.Join("\n• ", files.Take(15));
        var answer = System.Windows.MessageBox.Show(
            "DỌN RIÊNG BỘ VIỆT HÓA CŨ (ĐẶNG DEV / FORK):\n\n• " + preview
            + (files.Count > 15 ? $"\n... và {files.Count - 15} file khác." : "")
            + "\n\nChuyển file vào vùng cách ly, KHÔNG xoá vĩnh viễn. "
            + "Giữ nguyên PAK game, WAHU và mod không xác minh. Tiếp tục?",
            "Xóa xung đột Việt hóa cũ", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxResult.No);
        if (answer != System.Windows.MessageBoxResult.Yes) return;

        Busy = true;
        try
        {
            var result = _viet.QuarantineOldTranslationConflicts(game);
            if (!result.Success || result.Value is null)
            {
                Summary = "❌ " + result.Error;
                return;
            }
            LastQuarantinePath = result.Value.QuarantineDirectory;
            HasQuarantine = true;
            Summary = $"✅ Đã dọn {result.Value.MovedFiles.Count} file Việt hóa cũ vào cách ly. "
                + "File loader không xác minh và các mod khác được giữ nguyên.";
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

    private bool CanRestoreQuarantine() =>
        HasQuarantine && !Busy && !string.IsNullOrWhiteSpace(LastQuarantinePath);

    partial void OnHasQuarantineChanged(bool value) =>
        RestoreQuarantineCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanRestoreQuarantine))]
    private void RestoreQuarantine()
    {
        var game = _settings.Settings.GamePath;
        var path = LastQuarantinePath;
        if (string.IsNullOrWhiteSpace(game) || string.IsNullOrWhiteSpace(path)) return;

        var confirm = System.Windows.MessageBox.Show(
            "Khôi phục các mod đã cách ly vào thư mục game? "
            + "File mới hoặc file khác nội dung sẽ KHÔNG bị ghi đè.",
            "Khôi phục mod", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.No);
        if (confirm != System.Windows.MessageBoxResult.Yes) return;
        Busy = true;
        try
        {
            var restored = _viet.RestoreQuarantine(game, path);
            Summary = restored.Success
                ? $"Đã khôi phục {restored.Value} file; bản sao lưu vẫn được giữ."
                : "Không thể khôi phục: " + restored.Error;
        }
        finally { Busy = false; Refresh(); }
    }

    [RelayCommand(CanExecute = nameof(CanManageConflicts))]
    private void DeleteConflicts()
    {
        var game = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(game) || !HasConflicts)
        {
            Summary = "Không có mod xung đột để xóa.";
            return;
        }

        var currentConflicts = _viet.FindConflicts(game);
        if (currentConflicts.Count == 0)
        {
            Summary = "Không còn mod xung đột cần xóa.";
            Refresh();
            return;
        }
        var error = currentConflicts.FirstOrDefault(x => x.StartsWith("Không thể kiểm tra mod:", StringComparison.OrdinalIgnoreCase));
        if (error is not null)
        {
            Summary = "❌ " + error;
            return;
        }
        var preview = string.Join("\n• ", currentConflicts.Take(15));
        var answer = System.Windows.MessageBox.Show(
            "CÁCH LY (KHÔNG XÓA VĨNH VIỄN) các file mod đã phát hiện:\n• " + preview
            + (currentConflicts.Count > 15 ? $"\n... và {currentConflicts.Count - 15} mục khác." : "")
            + "\n\nMột số mod có thể không thực sự xung đột. Sao lưu SHA-256 và tạm "
            + "vô hiệu hóa các file trong danh sách, có thể khôi phục. Tiếp tục?",
            "Cách ly mod", System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning,
            System.Windows.MessageBoxResult.No);
        if (answer != System.Windows.MessageBoxResult.Yes) return;

        Busy = true;
        try
        {
            var result = _viet.DeleteConflicts(game);
            if (result.Success)
            {
                LastQuarantinePath = _viet.LastQuarantineDirectory ?? "";
                HasQuarantine = Directory.Exists(LastQuarantinePath);
            }
            Summary = result.Success
                ? $"Đã cách ly {result.Value} file mod, có thể khôi phục."
                : "❌ " + result.Error;
        }
        finally
        {
            Busy = false;
            Refresh();
        }
    }

    [ObservableProperty] private bool _hasHanViet;
    [ObservableProperty] private bool _showHanVietDownload = true;
    [ObservableProperty] private string _hanVietDownloadBtnText = "⬇️ Tải gói Hán Việt";

    private string HanVietPakPath => Path.Combine(AppContext.BaseDirectory, "content", "WuWaVH_HanViet_99_P.pak");

    private async Task<bool> DownloadHanVietInternalAsync(IProgress<InstallProgress>? prog = null, CancellationToken ct = default)
    {
        _lastHanVietDownloadError = "";
        var dst = HanVietPakPath;
        var version = typeof(InstallViewModel).Assembly.GetName().Version?.ToString(3) ?? "";
        if (string.IsNullOrWhiteSpace(version))
        {
            _lastHanVietDownloadError = "Không xác định được phiên bản ứng dụng.";
            return false;
        }

        try
        {
            // Khóa PAK Hán Việt vào đúng release tag của phiên bản app đang chạy.
            // Không dùng releases/latest vì app cũ có thể kéo nhầm dữ liệu của phiên bản tương lai.
            var release = await _update.GetReleaseManifestAsync(version, ct);
            if (!release.Success || release.Value is null)
            {
                _lastHanVietDownloadError = release.Error ?? $"Không đọc được release v{version}.";
                return false;
            }

            var manifest = release.Value;
            if (string.IsNullOrWhiteSpace(manifest.PakHanVietUrl)
                || manifest.PakHanVietSha256.Length != 64
                || !manifest.PakHanVietSha256.All(Uri.IsHexDigit))
            {
                _lastHanVietDownloadError = $"Release v{version} thiếu PAK Hán Việt hoặc SHA-256 hợp lệ.";
                return false;
            }

            var progress = new Progress<double>(p =>
                prog?.Report(new InstallProgress((int)Math.Round(p), $"Tải gói Hán Việt v{version}", 1, 2)));
            var download = await _update.DownloadFileAsync(
                manifest.PakHanVietUrl, manifest.PakHanVietSha256, dst, progress, ct);
            if (!download.Success || !File.Exists(dst))
            {
                _lastHanVietDownloadError = download.Error ?? "Không tải được PAK Hán Việt.";
                return false;
            }

            if (!PakV12Converter.TryVerifyV12(dst, out _))
            {
                File.Delete(dst);
                _lastHanVietDownloadError = "PAK Hán Việt tải về không phải PAK V12 hợp lệ.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _lastHanVietDownloadError = ex.Message;
            try { if (File.Exists(dst) && !PakV12Converter.TryVerifyV12(dst, out _)) File.Delete(dst); } catch { }
            return false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRefreshConflicts))]
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
            Summary = ok
                ? "✅ Đã tải xong gói Hán Việt đúng phiên bản! Bạn có thể bấm 'Cài Việt hóa' ngay."
                : "❌ Không thể tải gói Hán Việt đúng phiên bản: " + _lastHanVietDownloadError;
        }
        finally
        {
            Busy = false; _cts?.Dispose(); _cts = null; Refresh();
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartInstall))]
    private async Task InstallAsync()
    {
        var game = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(game)) { Summary = "Chưa chọn thư mục game (ở Trang chủ)."; return; }
        var r29 = R29MountedPak.ReleaseRequired(AppContext.BaseDirectory)
            || R29MountedPak.PackagePresent(AppContext.BaseDirectory);
        // Never block Install because an optional R29 variant/font is missing.
        // Make the selected UI match the variant actually being installed.
        var optionsAdjusted = r29 && VariantHanViet;
        if (r29)
        {
            VariantEnglish = true;
            ApplyFont = true;
        }
        var variant = VariantEnglish ? NameVariant.English : NameVariant.HanViet;
        string? quarantinedForInstall = null;
        if (r29)
        {
            if (_viet.GetStatus(game).LegacyFilesPresent)
            {
                Summary = "Còn PAK hoặc loader Việt hóa cũ. Hãy gỡ bộ cũ trước khi cài R29.";
                return;
            }
            var detected = _viet.FindConflicts(game);
            var scanError = detected.FirstOrDefault(x =>
                x.StartsWith("Không thể kiểm tra mod:", StringComparison.OrdinalIgnoreCase));
            if (scanError is not null) { Summary = "Không thể kiểm tra: " + scanError; return; }
            if (detected.Count > 0)
            {
                var preview = string.Join("\n• ", detected.Take(14));
                var confirm = System.Windows.MessageBox.Show(
                    "Phát hiện " + detected.Count + " tệp/bộ mod có thể xung đột:\n• "
                    + preview + (detected.Count > 14 ? "\n... và các mục khác." : "")
                    + "\n\nCác mod này không chắc đều gây lỗi. Nếu tiếp tục, chương trình sẽ "
                    + "SAO LƯU và TẠM VÔ HIỆU HÓA trước khi cài. Không xóa vĩnh viễn.\n\n"
                    + "Bạn đồng ý cách ly những mod được liệt kê?",
                    "Kiểm tra mod trước khi cài R29", System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning,
                    System.Windows.MessageBoxResult.No);
                if (confirm != System.Windows.MessageBoxResult.Yes)
                {
                    Summary = "Đã hủy cài. Không thay đổi mod hoặc tệp game.";
                    return;
                }
                var quarantine = _viet.QuarantineConflicts(game);
                if (!quarantine.Success || quarantine.Value is null)
                {
                    Summary = "Không thể sao lưu mod xung đột: " + quarantine.Error;
                    return;
                }
                quarantinedForInstall = quarantine.Value.QuarantineDirectory;
                LastQuarantinePath = quarantinedForInstall;
                HasQuarantine = true;
            }
        }
        _cts = new CancellationTokenSource();
        Busy = true; Progress = 0; ProgressText = "Bắt đầu...";
        Summary = r29 ? "Đang ghi đè R29 vào game..." : "Đang cài Việt hóa...";
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
                    Summary = "❌ Không thể tải gói Hán Việt đúng phiên bản: " + _lastHanVietDownloadError;
                    return;
                }
            }

            var r = await _viet.InstallAsync(game, variant, ApplyFont && FontAvailable, progress, _cts.Token);
            if (r.Success)
            {
                Summary = optionsAdjusted
                    ? "✅ Đã cài R29 tên Anh + Lexend 660 (Hán Việt chưa có cho R29)."
                    : "✅ Cài xong R29 + Lexend 660! Vào game đặt Text Language = English. Có thể dùng DirectX 11 hoặc 12.";
                if (quarantinedForInstall is not null)
                    Summary += " Đã cách ly mod có sao lưu. Dùng Khôi phục mod nếu cần.";
            }
            else
            {
                var error = r.Error?.StartsWith("❌") == true ? r.Error! : "❌ " + r.Error;
                if (quarantinedForInstall is not null)
                    error += "\nMod đã cách ly vẫn được sao lưu. Chọn Khôi phục mod để trả lại.";
                Summary = error;
                System.Windows.MessageBox.Show(
                    error,
                    "VHWuWa — Không thể cài đặt",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
        finally { Busy = false; _cts?.Dispose(); _cts = null; Refresh(); }
    }

    [RelayCommand(CanExecute = nameof(CanRefreshConflicts))]
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
            Summary = r.Success ? "✅ Đã gỡ Việt hóa và khôi phục file gốc." : (r.Error?.StartsWith("❌") == true ? r.Error : "❌ " + r.Error);
        }
        finally { Busy = false; Refresh(); }
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();
}
