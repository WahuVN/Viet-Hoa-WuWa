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
        var vst = (PathOk) ? _viet.GetStatus(GamePath) : null;
        if (vst is { Installed: true })
        {
            TranslationStatus = "Đã cài";
            TranslationVersion = string.IsNullOrWhiteSpace(vst.Version)
                ? "-"
                : "v" + vst.Version.TrimStart('v', 'V');
            CurrentVariant = vst.VariantLabel;
            CurrentFont = (PathOk ? _fonts.CurrentFontPak(GamePath) : null) ?? "Mặc định";
        }
        else
        {
            TranslationStatus = tr is null ? "Chưa cài" : "Đã cài";
            TranslationVersion = tr is null || string.IsNullOrWhiteSpace(tr.Version)
                ? "-"
                : "v" + tr.Version.TrimStart('v', 'V');
            CurrentVariant = "-";
            CurrentFont = "-";
        }
        _main.RefreshStatus();
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
        if (found.Count > 0) { SetPath(found[0]); Message = "Đã tìm thấy thư mục game."; }
        else Message = "Không tìm thấy game. Hãy chọn thư mục thủ công.";
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
    private async Task InstallAsync()
    {
        if (!PathOk) { Message = "Đường dẫn game chưa hợp lệ."; return; }
        var dlg = new OpenFileDialog { Title = "Chọn gói Việt hóa", Filter = "Gói VHWuWa (*.vhwpack)|*.vhwpack" };
        if (dlg.ShowDialog() != true) return;
        Busy = true; Message = "Đang cài Việt hóa...";
        try
        {
            var r = await _installer.InstallAsync(GamePath, dlg.FileName);
            Message = r.Success ? "Cài Việt hóa thành công." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; Refresh(); }
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
