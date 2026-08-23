using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using Wpf.Ui.Appearance;

namespace VHWuWa.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IGameDetectionService _detect;
    private readonly IUpdateService _update;
    private readonly ILogService _log;

    [ObservableProperty] private string _appVersion;
    [ObservableProperty] private string _gameStatus = "Chưa chọn thư mục game";
    [ObservableProperty] private bool _gameOk;
    [ObservableProperty] private string _updateStatus = "Chưa kiểm tra cập nhật";
    [ObservableProperty] private bool _isDark = true;

    [ObservableProperty] private bool _hasUpdate;
    [ObservableProperty] private string _latestVersion = "";
    [ObservableProperty] private string _updateTitle = "";
    [ObservableProperty] private string _updateNotes = "";
    [ObservableProperty] private double _updateProgress;
    [ObservableProperty] private bool _isUpdating;
    [ObservableProperty] private string _updateStatusMessage = "";

    public UpdateManifest? CurrentUpdateManifest { get; set; }

    /// <summary>Bản dịch đi kèm: v2.0.0, dành cho game 3.6.</summary>
    public string VhVersion => "Bản dịch v2.0.0 · game 3.6";

    public MainViewModel(ISettingsService settings, IGameDetectionService detect, IUpdateService update, ILogService log)
    {
        _settings = settings;
        _detect = detect;
        _update = update;
        _log = log;
        _appVersion = "VHWuWa v" + (Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "2.0.0");
        _isDark = !settings.Settings.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        RefreshStatus();

        if (settings.Settings.AutoCheckUpdate)
        {
            _ = Task.Run(() => CheckUpdateAsync(true));
        }
    }

    public void RefreshStatus()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            GameOk = false;
            GameStatus = "Chưa chọn thư mục game";
            return;
        }
        var v = _detect.Validate(path);
        GameOk = v.IsValid;
        GameStatus = v.IsValid ? "Game hợp lệ" : "Đường dẫn game chưa hợp lệ";
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDark = !IsDark;
        var theme = IsDark ? "Dark" : "Light";
        _settings.Settings.Theme = theme;
        _settings.Save();
        ApplicationThemeManager.Apply(IsDark ? ApplicationTheme.Dark : ApplicationTheme.Light);
    }

    [RelayCommand]
    public async Task<UpdateCheckResult> CheckUpdateAsync(bool silent = false)
    {
        if (!silent) UpdateStatusMessage = "Đang kiểm tra cập nhật từ máy chủ...";
        try
        {
            var r = await _update.CheckAsync();
            if (r.UpdateAvailable && r.Manifest != null)
            {
                CurrentUpdateManifest = r.Manifest;
                LatestVersion = r.Manifest.Version;
                UpdateTitle = $"🎉 Đã có bản cập nhật mới v{r.Manifest.Version}!";
                UpdateNotes = string.IsNullOrWhiteSpace(r.Manifest.ReleaseNotes)
                    ? "Có phiên bản mới được phát hành. Bấm nút bên dưới để tự động tải và cập nhật ngay."
                    : r.Manifest.ReleaseNotes;
                HasUpdate = true;
                UpdateStatus = $"Có bản mới v{r.Manifest.Version}";
                UpdateStatusMessage = r.Message;
            }
            else
            {
                HasUpdate = false;
                UpdateStatus = "Đã là bản mới nhất";
                UpdateStatusMessage = r.Message;
            }
            return r;
        }
        catch (Exception ex)
        {
            _log.Warn("Update", "Lỗi kiểm tra cập nhật: " + ex.Message);
            UpdateStatusMessage = "Không thể kiểm tra cập nhật: " + ex.Message;
            return new UpdateCheckResult { Message = ex.Message };
        }
    }

    [RelayCommand]
    public async Task ApplyUpdateAsync()
    {
        if (IsUpdating || CurrentUpdateManifest == null) return;
        if (string.IsNullOrWhiteSpace(CurrentUpdateManifest.DownloadUrl))
        {
            UpdateStatusMessage = "Không tìm thấy đường dẫn tải về trong gói phát hành.";
            return;
        }

        IsUpdating = true;
        UpdateProgress = 0;
        UpdateStatusMessage = "Đang tải bản cập nhật mới...";
        try
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "VHWuWa_Update_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            var progress = new Progress<double>(p =>
            {
                UpdateProgress = p;
                UpdateStatusMessage = $"Đang tải bản cập nhật: {p:F0}%";
            });

            var dlRes = await _update.DownloadAsync(CurrentUpdateManifest, tempDir, progress);
            if (!dlRes.Success || string.IsNullOrEmpty(dlRes.Value))
            {
                UpdateStatusMessage = "Tải bản cập nhật thất bại: " + dlRes.Error;
                return;
            }

            UpdateStatusMessage = "Đang giải nén và cập nhật...";
            var zipPath = dlRes.Value;
            var appDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
            var updaterExe = Path.Combine(appDir, "VHWuWa.Updater.exe");
            var pid = Environment.ProcessId;

            if (File.Exists(updaterExe))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = updaterExe,
                    Arguments = $"--zip \"{zipPath}\" --target \"{appDir}\" --relaunch \"VHWuWa.exe\" --pid {pid}",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            else
            {
                // Fallback updater script nếu không có file updater exe rời
                var scriptPath = Path.Combine(tempDir, "apply_update.bat");
                var batContent = $"@echo off\r\nchcp 65001 >nul\r\ntimeout /t 2 /nobreak >nul\r\npowershell -NoProfile -ExecutionPolicy Bypass -Command \"Expand-Archive -LiteralPath '{zipPath}' -DestinationPath '{appDir}' -Force\"\r\nstart \"\" \"{Path.Combine(appDir, "VHWuWa.exe")}\"\r\n";
                File.WriteAllText(scriptPath, batContent, System.Text.Encoding.ASCII);
                Process.Start(new ProcessStartInfo(scriptPath) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden });
            }

            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _log.Error("Update", "Lỗi tự động cập nhật: " + ex.Message, ex);
            UpdateStatusMessage = "Lỗi tự động cập nhật: " + ex.Message;
        }
        finally
        {
            IsUpdating = false;
        }
    }
}

