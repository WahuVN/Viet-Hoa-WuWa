using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IGameDetectionService _detect;
    private readonly IGameLaunchService _gameLaunch;
    private readonly IPackageInstallerService _installer;
    private readonly IViethoaInstaller _viet;
    private readonly IFontService _fonts;
    private readonly ILogService _log;
    private readonly MainViewModel _main;

    [ObservableProperty] private string _gamePath = "";
    [ObservableProperty] private string _gameName = "";
    [ObservableProperty] private string _detectedVersion = "Chưa xác định";
    [ObservableProperty] private string _translationStatus = "Chưa cài";
    [ObservableProperty] private string _translationVersion = "-";
    [ObservableProperty] private string _currentVariant = "-";
    [ObservableProperty] private string _currentFont = "-";
    [ObservableProperty] private string _pathStatus = "Chưa chọn";
    [ObservableProperty] private bool _pathOk;
    [ObservableProperty] private bool _isInstalled;
    [ObservableProperty] private bool _canOneClickInstall;
    [ObservableProperty] private string _message = "Sẵn sàng.";
    [ObservableProperty] private bool _busy;
    [ObservableProperty] private bool _forceCSharpEnvironment;
    [ObservableProperty] private string _launchButtonText = "Mở game";

    public MainViewModel Main => _main;

    public HomeViewModel(ISettingsService settings, IGameDetectionService detect, IGameLaunchService gameLaunch,
        IPackageInstallerService installer, IViethoaInstaller viet, IFontService fonts, ILogService log, MainViewModel main)
    {
        _settings = settings; _detect = detect; _gameLaunch = gameLaunch; _installer = installer; _viet = viet; _fonts = fonts; _log = log; _main = main;
        GameName = string.IsNullOrWhiteSpace(detect.GameConfig.GameName) ? "Wuthering Waves" : detect.GameConfig.GameName;
        ForceCSharpEnvironment = settings.Settings.ForceCSharpEnvironment;
        OnActivated();
    }

    public void OnActivated()
    {
        GamePath = _settings.Settings.GamePath;
        // Tự động tìm thư mục game nếu chưa chọn hoặc đường dẫn hiện tại không hợp lệ
        if (string.IsNullOrWhiteSpace(GamePath) || !_detect.Validate(GamePath).IsValid)
        {
            var found = _detect.AutoDetect();
            if (found.Count > 0)
            {
                SetPath(found[0]);
            }
        }
        Refresh();
    }

    private void Refresh()
    {
        var v = _detect.Validate(GamePath);
        PathOk = v.IsValid;
        PathStatus = string.IsNullOrWhiteSpace(GamePath) ? "Chưa chọn"
            : v.IsValid ? "Hợp lệ" : v.Message;
        DetectedVersion = v.DetectedVersion ?? "Chưa xác định";

        var state = _settings.LoadState();
        var tr = state.InstalledPackages.FirstOrDefault(p => p.PackageType == PackageType.Translation);
        var vst = PathOk ? _viet.GetStatus(GamePath) : null;
        if (vst is { Installed: true })
        {
            IsInstalled = true;
            TranslationStatus = "Đã cài";
            TranslationVersion = string.IsNullOrWhiteSpace(vst.Version)
                ? "-"
                : "v" + vst.Version.TrimStart('v', 'V');
            CurrentVariant = vst.VariantLabel;
            CurrentFont = (PathOk ? _fonts.CurrentFontPak(GamePath) : null) ?? "Mặc định";
            CanOneClickInstall = false;
        }
        else
        {
            IsInstalled = false;
            TranslationStatus = tr is null ? "Chưa cài" : "Đã cài";
            TranslationVersion = tr is null || string.IsNullOrWhiteSpace(tr.Version)
                ? "-"
                : "v" + tr.Version.TrimStart('v', 'V');
            CurrentVariant = "-";
            CurrentFont = "-";
            CanOneClickInstall = PathOk;
        }

        if (PathOk && !IsInstalled)
        {
            Message = "⚡ Đã tìm thấy thư mục game! Bấm “Cài Việt Hóa (Tự Động)” để thiết lập trọn gói.";
        }
        else if (PathOk && IsInstalled)
        {
            Message = "✅ Đã cài Việt hóa. Sẵn sàng vào game!";
        }

        _main.RefreshStatus();
    }

    [RelayCommand]
    private async Task OneClickInstallAsync()
    {
        if (!PathOk)
        {
            Message = "Đường dẫn game chưa hợp lệ. Hãy dùng Tự tìm hoặc chọn thư mục game.";
            return;
        }

        var res = MessageBox.Show(
            $"Bạn có muốn tự động cài đặt bản Việt Hóa chuẩn vào thư mục game:\n{GamePath}\n\n(Hệ thống sẽ tự động cài file PAK Việt Hóa, Font chữ và Loader chống lỗi).",
            "Cài đặt Việt Hóa tự động",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Information);

        if (res != MessageBoxResult.OK && res != MessageBoxResult.Yes) return;

        Busy = true;
        Message = "Đang tự động cài đặt Việt hóa vào game…";
        try
        {
            var r = await _viet.InstallAsync(GamePath, NameVariant.English, withFont: true);
            if (r.Success)
            {
                Message = "✅ Cài đặt Việt Hóa thành công! Bạn có thể bấm “Mở game” ngay bây giờ.";
                MessageBox.Show(
                    "Cài đặt Việt Hóa thành công 100%!\nBạn có thể bấm “Mở game” để trải nghiệm ngay.",
                    "VHWuWa — Cài đặt hoàn tất",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                var err = VHWuWa.Core.Services.ErrorFormatter.Humanize(r.Error);
                Message = "❌ " + err;
                MessageBox.Show(err, "VHWuWa — Lỗi cài đặt", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            var err = VHWuWa.Core.Services.ErrorFormatter.Humanize(null, ex);
            Message = "❌ " + err;
            MessageBox.Show(err, "VHWuWa — Lỗi cài đặt", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Busy = false;
            Refresh();
        }
    }

    [RelayCommand]
    private async Task OneClickInstallHvAsync()
    {
        if (!PathOk) return;
        var res = MessageBox.Show(
            $"Bạn có muốn tự động cài đặt bản Hán Việt vào thư mục game:\n{GamePath}?",
            "Cài đặt Hán Việt tự động",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Information);

        if (res != MessageBoxResult.OK && res != MessageBoxResult.Yes) return;

        Busy = true;
        Message = "Đang tự động cài đặt bản Hán Việt vào game…";
        try
        {
            var r = await _viet.InstallAsync(GamePath, NameVariant.HanViet, withFont: true);
            if (r.Success)
            {
                Message = "✅ Cài đặt bản Hán Việt thành công! Bạn có thể bấm “Mở game” ngay bây giờ.";
                MessageBox.Show(
                    "Cài đặt bản Hán Việt thành công 100%!\nBạn có thể bấm “Mở game” để trải nghiệm ngay.",
                    "VHWuWa — Cài đặt hoàn tất",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                var err = VHWuWa.Core.Services.ErrorFormatter.Humanize(r.Error);
                Message = "❌ " + err;
                MessageBox.Show(err, "VHWuWa — Lỗi cài đặt", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            var err = VHWuWa.Core.Services.ErrorFormatter.Humanize(null, ex);
            Message = "❌ " + err;
            MessageBox.Show(err, "VHWuWa — Lỗi cài đặt", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            Busy = false;
            Refresh();
        }
    }

    [RelayCommand]
    private void ChooseFolder()
    {
        var dlg = new OpenFolderDialog { Title = "Chọn thư mục game (vd: D:\\Game\\Wuthering Waves Game - chứa Client)" };
        if (dlg.ShowDialog() == true)
        {
            var p = _detect.NormalizeGamePath(dlg.FolderName) ?? dlg.FolderName;
            SetPath(p);
        }
    }

    partial void OnForceCSharpEnvironmentChanged(bool value)
    {
        _settings.Settings.ForceCSharpEnvironment = value;
        _settings.Save();
        LaunchButtonText = value ? "Mở game · C#" : "Mở game";
    }

    [RelayCommand]
    private void LaunchGame()
    {
        if (!PathOk)
        {
            Message = "Đường dẫn game chưa hợp lệ. Hãy dùng Tự tìm game hoặc chọn lại thư mục.";
            return;
        }

        if (ForceCSharpEnvironment && !_settings.Settings.CSharpLaunchWarningAccepted)
        {
            var accepted = MessageBox.Show(
                "Đây là môi trường C# thử nghiệm của game, không bảo đảm tăng FPS trên mọi máy. " +
                "Lần chạy đầu có thể tải thêm dữ liệu; nếu giật, crash hoặc lỗi, hãy tắt công tắc để trở về chế độ thường.\n\n" +
                "Khi kích hoạt thành công, cuối phiên bản trong game sẽ có dấu *. Bạn có muốn tiếp tục?",
                "Môi trường C# thử nghiệm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (accepted != MessageBoxResult.Yes) return;
            _settings.Settings.CSharpLaunchWarningAccepted = true;
            _settings.Save();
        }

        var result = _gameLaunch.Launch(GamePath, ForceCSharpEnvironment);
        Message = result.Success
            ? ForceCSharpEnvironment
                ? "Đã mở game với môi trường C# thử nghiệm."
                : "Game đã được mở."
            : "Không thể mở game: " + result.Error;
    }

    [RelayCommand]
    private void CopyCSharpArgument()
    {
        Clipboard.SetText(GameLaunchOptions.ForceCSharpEnvironment);
        Message = "Đã sao chép tham số. Dán vào Steam → Properties → Launch Options.";
    }

    [RelayCommand]
    private void AutoDetect()
    {
        var found = _detect.AutoDetect();
        if (found.Count > 0)
        {
            SetPath(found[0]);
            Message = "Đã tìm thấy thư mục game.";
        }
        else
        {
            Message = "Không tìm thấy game. Hãy chọn thư mục thủ công.";
        }
    }

    public void SetPath(string path)
    {
        GamePath = _detect.NormalizeGamePath(path) ?? path.Trim().TrimEnd('\\', '/');
        _settings.Settings.GamePath = GamePath;
        _settings.Save();
        Refresh();
    }

    [RelayCommand]
    private void CheckFiles()
    {
        var v = _detect.Validate(GamePath);
        Message = v.IsValid ? "Các file game cần thiết đều đầy đủ." : v.Message;
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (Directory.Exists(GamePath))
            Process.Start(new ProcessStartInfo(GamePath) { UseShellExecute = true });
        else Message = "Thư mục game không tồn tại.";
    }

    [RelayCommand]
    private void KillGameProcesses()
    {
        try
        {
            var procs = Process.GetProcessesByName("Client-Win64-Shipping")
                .Concat(Process.GetProcessesByName("Wuthering Waves")).ToList();
            if (procs.Count == 0)
            {
                Message = "Game hiện không chạy.";
                return;
            }
            int count = 0;
            foreach (var p in procs)
            {
                try { p.Kill(true); count++; } catch { }
            }
            Message = count > 0 ? "Đã tắt game." : "Không thể đóng tiến trình game.";
        }
        catch (Exception ex)
        {
            Message = "Lỗi khi đóng game: " + ex.Message;
        }
    }
}
