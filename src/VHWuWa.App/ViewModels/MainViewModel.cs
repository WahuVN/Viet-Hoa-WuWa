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
    private const string DiscordUrl = "https://discord.gg/tuRCj47sy";
    private const string GitHubUrl = "https://github.com/WahuVN/Viet-Hoa-WuWa";
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

    /// <summary>Phiên bản bản dịch đi cùng bản build hiện tại.</summary>
    public string VhVersion => "Bản dịch v"
        + (Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "2.0.0")
        + " · game 3.6";

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
            _ = CheckUpdateAsync(true);
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
    private void OpenDiscord() => OpenExternalUrl(DiscordUrl);

    [RelayCommand]
    private void OpenGitHub() => OpenExternalUrl(GitHubUrl);

    private void OpenExternalUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Warn("Link", $"Không mở được {url}: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task<UpdateCheckResult> CheckUpdateAsync(bool silent = false)
    {
        if (!silent) UpdateStatusMessage = "Đang kiểm tra cập nhật từ máy chủ...";
        try
        {
            var r = await _update.CheckAsync();
            if (!r.CheckSucceeded)
            {
                HasUpdate = false;
                UpdateStatus = "Không kiểm tra được";
                UpdateStatusMessage = r.Message;
            }
            else if (r.UpdateAvailable && r.Manifest != null)
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
            _settings.Settings.LastUpdateCheck = DateTimeOffset.UtcNow;
            _settings.Save();
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
            var pid = Environment.ProcessId;

            // Dùng PowerShell có sẵn trên Windows thay cho một updater self-contained
            // mang lặp toàn bộ runtime .NET. Script vẫn kiểm tra ZIP, sao lưu và
            // rollback trước khi mở lại app.
            var scriptPath = Path.Combine(tempDir, "apply_update.ps1");
            var script = """
                param([string]$Zip,[string]$Target,[int]$AppPid)
                $ErrorActionPreference='Stop'
                try { Wait-Process -Id $AppPid -Timeout 30 -ErrorAction SilentlyContinue } catch {}
                $stage=Join-Path $env:TEMP ('VHWuWa_Extract_'+[guid]::NewGuid().ToString('N'))
                $backup=$Target.TrimEnd('\\','/')+'_backup_'+(Get-Date -Format 'yyyyMMdd_HHmmss')
                try {
                  Expand-Archive -LiteralPath $Zip -DestinationPath $stage -Force
                  $exe=Get-ChildItem -LiteralPath $stage -Recurse -Filter 'VHWuWa.exe' -File | Where-Object { $_.Directory.Name -ieq 'app' } | Sort-Object { $_.FullName.Length } | Select-Object -First 1
                  if(-not $exe){$exe=Get-Item -LiteralPath (Join-Path $stage 'VHWuWa.exe') -ErrorAction SilentlyContinue}
                  if(-not $exe){throw 'ZIP cập nhật không có VHWuWa.exe hợp lệ'}
                  New-Item -ItemType Directory -Path $backup -Force | Out-Null
                  Get-ChildItem -LiteralPath $Target -Force | Copy-Item -Destination $backup -Recurse -Force
                  Get-ChildItem -LiteralPath $exe.Directory.FullName -Force | Copy-Item -Destination $Target -Recurse -Force
                  if(-not (Test-Path -LiteralPath (Join-Path $Target 'VHWuWa.exe'))){throw 'Thiếu VHWuWa.exe sau cập nhật'}
                  Remove-Item -LiteralPath (Join-Path $Target 'VHWuWa.Updater.exe') -Force -ErrorAction SilentlyContinue
                  Remove-Item -LiteralPath (Join-Path $Target 'VHWuWa.Updater.next.exe') -Force -ErrorAction SilentlyContinue
                  Start-Process -FilePath (Join-Path $Target 'VHWuWa.exe') -WorkingDirectory $Target
                  Remove-Item -LiteralPath $backup -Recurse -Force -ErrorAction SilentlyContinue
                } catch {
                  if(Test-Path -LiteralPath $backup){
                    Get-ChildItem -LiteralPath $backup -Force | Copy-Item -Destination $Target -Recurse -Force
                    if(Test-Path -LiteralPath (Join-Path $Target 'VHWuWa.exe')){Start-Process -FilePath (Join-Path $Target 'VHWuWa.exe') -WorkingDirectory $Target}
                  }
                  throw
                } finally {
                  Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
                }
                """;
            File.WriteAllText(scriptPath, script, new System.Text.UTF8Encoding(true));
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Zip \"{zipPath}\" -Target \"{appDir}\" -AppPid {pid}",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = tempDir
            });

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

