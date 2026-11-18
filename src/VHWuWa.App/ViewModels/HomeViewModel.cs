using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;
using VHWuWa.Infrastructure;

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
    [ObservableProperty] private bool _hasStagedContent;
    [ObservableProperty] private string _stagedContentText = "";
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

    private DateTime _nextAutoDetectUtc = DateTime.MinValue;

    // Read the release marker instead of presenting the app version as the game-data version.
    public string FeaturedReleaseTitle { get; private set; } = "Wuthering Waves";
    public string FeaturedReleaseBadge { get; private set; } = "Chưa xác định dữ liệu";
    public bool ShowFeaturedSeasonTitle { get; private set; } = false;
    public string FeaturedReleaseLineOne { get; private set; } = "";
    public string FeaturedReleaseLineTwo { get; private set; } = "";

    public MainViewModel Main => _main;

    public HomeViewModel(ISettingsService settings, IGameDetectionService detect, IGameLaunchService gameLaunch,
        IPackageInstallerService installer, IViethoaInstaller viet, IFontService fonts, ILogService log, MainViewModel main)
    {
        _settings = settings; _detect = detect; _gameLaunch = gameLaunch; _installer = installer; _viet = viet; _fonts = fonts; _log = log; _main = main;
        GameName = string.IsNullOrWhiteSpace(detect.GameConfig.GameName) ? "Wuthering Waves" : detect.GameConfig.GameName;
        // WuWa 3.7 must start through the official Kuro launcher. The old experimental
        // C# client flag is no longer user-facing and must not leak from previous settings.
        ForceCSharpEnvironment = false;
        settings.Settings.ForceCSharpEnvironment = false;
        settings.Save();
        LaunchButtonText = "Mở game";
        LoadFeaturedRelease();
        OnActivated();
    }

    private void LoadFeaturedRelease()
    {
        // The text is local package metadata, never a network request or guessed game version.
        try
        {
            var marker = Path.Combine(AppContext.BaseDirectory, "content", "r29-release.json");
            if (!File.Exists(marker)) return;
            using var metadata = JsonDocument.Parse(File.ReadAllText(marker));
            var root = metadata.RootElement;
            var resourceVersion = root.TryGetProperty("resourceVersion", out var resource)
                ? resource.GetString() : null;
            var release = root.TryGetProperty("translationRelease", out var pack)
                ? pack.GetString() : null;
            if (string.IsNullOrWhiteSpace(resourceVersion)) return;
            FeaturedReleaseBadge = string.IsNullOrWhiteSpace(release)
                ? resourceVersion
                : $"{resourceVersion}  ·  {release}";
            // The original clip and reference image identify this season only for 3.8.11.
            FeaturedReleaseTitle = resourceVersion == "3.8.11"
                ? "Bách Hợp Giữa Mộng Tàn"
                : "Wuthering Waves";
            ShowFeaturedSeasonTitle = resourceVersion == "3.8.11";
            FeaturedReleaseLineOne = ShowFeaturedSeasonTitle ? "Bách Hợp" : "";
            FeaturedReleaseLineTwo = ShowFeaturedSeasonTitle ? "Giữa Mộng Tàn" : "";
        }
        catch (Exception ex)
        {
            _log.Warn("UI", "Không đọc được tên phiên bản trên bản phát hành: " + ex.Message);
        }
    }

    public void OnActivated()
    {
        GamePath = _settings.Settings.GamePath;
        // Avoid blocking every tab switch with repeated drive/Steam detection.
        // The explicit "Tự tìm" command always performs a fresh scan.
        if ((string.IsNullOrWhiteSpace(GamePath) || !_detect.Validate(GamePath).IsValid)
            && DateTime.UtcNow >= _nextAutoDetectUtc)
        {
            _nextAutoDetectUtc = DateTime.UtcNow.AddMinutes(2);
            var found = _detect.AutoDetect();
            if (found.Count > 0)
                SetPath(found[0]);
        }
        Refresh();
    }

    private void Refresh()
    {
        var v = _detect.Validate(GamePath);
        PathOk = v.IsValid;
        PathStatus = string.IsNullOrWhiteSpace(GamePath) ? "Chưa chọn"
            : v.IsValid ? "Hợp lệ" : v.Message;
        var resourceVersion = PathOk
            ? GameDetectionService.DetectResourceVersion(GamePath, v.DetectedVersion)
            : null;
        DetectedVersion = (v.DetectedVersion ?? "Chưa xác định")
            + (resourceVersion is null ? "" : " · dữ liệu " + resourceVersion);
        var r28KnownFailure = v.DetectedVersion == "3.8.0" && resourceVersion == "3.8.11";

        // A stale package record cannot confirm an installation in a missing game folder.
        var vst = PathOk ? _viet.GetStatus(GamePath) : null;
        if (vst is { Installed: true })
        {
            IsInstalled = true;
            TranslationStatus = "Đã cài";
            TranslationVersion = string.IsNullOrWhiteSpace(vst.Version)
                ? "-"
                : vst.Version.StartsWith("R", StringComparison.Ordinal) ? vst.Version
                : "v" + vst.Version.TrimStart('v', 'V');
            CurrentVariant = vst.VariantLabel;
            CurrentFont = (PathOk ? _fonts.CurrentFontPak(GamePath) : null) ?? "Mặc định";
            CanOneClickInstall = false;
        }
        else
        {
            IsInstalled = false;
            TranslationStatus = PathOk ? "Chưa cài" : "Chưa tìm thấy game";
            TranslationVersion = "-";
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

        var staged = StagedPakInspector.Inspect(AppContext.BaseDirectory);
        var r29Present = R29MountedPak.ReleaseRequired(AppContext.BaseDirectory)
            || R29MountedPak.PackagePresent(AppContext.BaseDirectory);
        HasStagedContent = r29Present || staged.Present;
        StagedContentText = r29Present
            ? (!PathOk ? "Chọn thư mục game để cài."
                : "R29 sẵn sàng ghi đè trực tiếp.")
            : staged.ShaVerified
            ? (r28KnownFailure
                ? "PAK R28 (dữ liệu 3.8.10) đã gây lỗi Corrupt pak index trên PC 3.8.0/dữ liệu 3.8.11. Chưa cài được."
                : "PAK R28 · 62 DB · SHA-256 hợp lệ; chưa được game PC xác nhận tương thích.")
            : staged.Message;
        if (r29Present)
        {
            // Always allow the user to press Install once the game client is found.
            // The installer will self-repair optional folders and handle conflicts.
            CanOneClickInstall = PathOk;
            Message = IsInstalled ? "Đã cài Việt hóa R29 qua Mount." : StagedContentText;
        }
        else if (staged.Present)
        {
            CanOneClickInstall = false;
            Message = !PathOk
                ? "Chưa tìm thấy game PC. Chọn thư mục chứa Client để cài."
                : staged.ShaVerified
                    ? StagedContentText
                    : staged.Message;
        }

        _main.RefreshStatus();
    }

    [RelayCommand]
    private async Task OneClickInstallAsync()
    {
        if (!R29MountedPak.ReleaseRequired(AppContext.BaseDirectory)
            && !R29MountedPak.PackagePresent(AppContext.BaseDirectory)
            && StagedPakInspector.Inspect(AppContext.BaseDirectory).Present)
        {
            Message = StagedContentText;
            return;
        }
        if (!PathOk)
        {
            Message = "Đường dẫn game chưa hợp lệ. Hãy dùng Tự tìm hoặc chọn thư mục game.";
            return;
        }

        // R29 direct-insert: chỉ ghi đè các tệp WAHU quản lý, không quét/dọn mod khác.
        var isR29 = R29MountedPak.ReleaseRequired(AppContext.BaseDirectory)
            || R29MountedPak.PackagePresent(AppContext.BaseDirectory);
        if (!isR29)
        {
            var res = MessageBox.Show(
                $"Bạn muốn cài Việt hóa vào thư mục game:\n{GamePath}?",
                "Cài đặt Việt hóa",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Information);
            if (res != MessageBoxResult.OK && res != MessageBoxResult.Yes) return;
        }

        Busy = true;
        Message = "Đang tự động cài đặt Việt hóa vào game…";
        try
        {
            var r = await _viet.InstallAsync(GamePath, NameVariant.English, withFont: !isR29);
            if (r.Success)
            {
                Message = "Đã ghi đè PAK Việt hóa. Có thể mở game.";
                MessageBox.Show(
                    "Đã ghi đè Việt hóa vào game.",
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
        if (R29MountedPak.ReleaseRequired(AppContext.BaseDirectory)
            || StagedPakInspector.Inspect(AppContext.BaseDirectory).Present)
        {
            Message = "R29 chỉ có bản giữ tên quốc tế; chưa có PAK Hán Việt tương thích.";
            return;
        }
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
            var launcher = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Wuthering Waves", "launcher.exe");
            if (!File.Exists(launcher))
            {
                Message = "Chưa tìm thấy game PC hoặc launcher. Cài game rồi chọn thư mục chứa Client.";
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo(launcher)
                {
                    WorkingDirectory = Path.GetDirectoryName(launcher) ?? "",
                    UseShellExecute = true
                });
                Message = "Đã mở launcher. Cài game xong hãy bấm Tự tìm game.";
            }
            catch (Exception ex)
            {
                Message = "Không thể mở launcher: " + ex.Message;
            }
            return;
        }

        var result = _gameLaunch.Launch(GamePath, forceCSharpEnvironment: false);
        Message = result.Success
            ? "Đã mở launcher Wuthering Waves. Bấm Start trong launcher để vào game."
            : "Không thể mở launcher: " + result.Error;
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
        _nextAutoDetectUtc = DateTime.UtcNow.AddMinutes(2);
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
        _nextAutoDetectUtc = DateTime.MinValue;
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
