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
    private readonly IViethoaInstaller? _vietHoa;

    [ObservableProperty] private string _appVersion;
    [ObservableProperty] private string _gameStatus = "Chưa chọn thư mục game";
    [ObservableProperty] private bool _gameOk;
    [ObservableProperty] private string _updateStatus = "Chưa kiểm tra cập nhật";
    [ObservableProperty] private bool _isDark = true;

    [ObservableProperty] private bool _hasUpdate;
    [ObservableProperty] private bool _hasDeltaPak;
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

    public MainViewModel(ISettingsService settings, IGameDetectionService detect, IUpdateService update, ILogService log, IViethoaInstaller? vietHoa = null)
    {
        _settings = settings;
        _detect = detect;
        _update = update;
        _log = log;
        _vietHoa = vietHoa;
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
                HasDeltaPak = false;
                UpdateStatus = "Không kiểm tra được";
                UpdateStatusMessage = r.Message;
            }
            else if (r.UpdateAvailable && r.Manifest != null)
            {
                CurrentUpdateManifest = r.Manifest;
                LatestVersion = r.Manifest.Version;
                HasDeltaPak = r.Manifest.HasDeltaPak;
                UpdateTitle = $"🎉 Đã có bản cập nhật mới v{r.Manifest.Version}!";
                UpdateNotes = string.IsNullOrWhiteSpace(r.Manifest.ReleaseNotes)
                    ? "Có phiên bản mới được phát hành. Bấm nút bên dưới để tự động tải và cập nhật ngay."
                    : r.Manifest.ReleaseNotes;
                HasUpdate = true;
                UpdateStatus = $"Có bản mới v{r.Manifest.Version}";
                UpdateStatusMessage = r.Message;

                if (!silent)
                {
                    var promptMsg = $"Đã tìm thấy bản cập nhật mới v{r.Manifest.Version}!\n\n" +
                                    $"Ghi chú:\n{UpdateNotes}\n\n" +
                                    "Bạn có muốn tự động tải và cài đặt bản cập nhật ngay bây giờ không?";

                    var ask = System.Windows.MessageBox.Show(
                        promptMsg,
                        "VHWuWa — Cập Nhật",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Information);

                    if (ask == System.Windows.MessageBoxResult.Yes)
                    {
                        _ = ApplyUpdateAsync();
                    }
                }
            }
            else
            {
                HasUpdate = false;
                HasDeltaPak = false;
                UpdateStatus = "Đã là bản mới nhất";
                UpdateStatusMessage = r.Message;
                if (!silent)
                {
                    System.Windows.MessageBox.Show(
                        "Bạn đang sử dụng phiên bản mới nhất.",
                        "VHWuWa — Cập Nhật",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            _settings.Settings.LastUpdateCheck = DateTimeOffset.UtcNow;
            _settings.Save();
            return r;
        }
        catch (Exception ex)
        {
            _log.Warn("Update", "Lỗi kiểm tra cập nhật: " + ex.Message);
            UpdateStatusMessage = "Không thể kiểm tra cập nhật: " + ex.Message;
            if (!silent)
            {
                System.Windows.MessageBox.Show(
                    "Không thể kiểm tra cập nhật:\n" + ex.Message,
                    "VHWuWa — Lỗi Cập Nhật",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
            return new UpdateCheckResult { Message = ex.Message };
        }
    }

    [RelayCommand]
    public async Task ApplyDeltaPakUpdateAsync()
    {
        if (IsUpdating || CurrentUpdateManifest == null) return;
        var pakEnUrl = CurrentUpdateManifest.PakEnUrl;
        var pakHvUrl = CurrentUpdateManifest.PakHanVietUrl;

        if (string.IsNullOrWhiteSpace(pakEnUrl) && string.IsNullOrWhiteSpace(pakHvUrl))
        {
            UpdateStatusMessage = "Release này không có tệp PAK riêng lẻ để cập nhật nhanh.";
            return;
        }

        IsUpdating = true;
        UpdateProgress = 0;
        UpdateStatusMessage = "Đang tải tệp Việt hóa mới nhất...";

        try
        {
            var appDir = AppContext.BaseDirectory;
            var contentDir = Path.Combine(appDir, "content");
            Directory.CreateDirectory(contentDir);

            // 1. Tải PAK EN
            if (!string.IsNullOrWhiteSpace(pakEnUrl))
            {
                UpdateStatusMessage = "Đang tải tệp Việt hóa (Tiếng Anh)...";
                var destEn = Path.Combine(contentDir, "WuWaVH_EN_99_P.pak");
                var progress = new Progress<double>(p =>
                {
                    UpdateProgress = p;
                    UpdateStatusMessage = $"Đang tải PAK EN: {p:F0}%";
                });
                var rEn = await _update.DownloadFileAsync(pakEnUrl, CurrentUpdateManifest.PakEnSha256, destEn, progress);
                if (!rEn.Success)
                {
                    throw new Exception("Lỗi tải bản dịch EN: " + rEn.Error);
                }
            }

            // 2. Tải PAK Hán Việt
            if (!string.IsNullOrWhiteSpace(pakHvUrl))
            {
                UpdateStatusMessage = "Đang tải tệp Việt hóa (Hán Việt)...";
                var destHv = Path.Combine(contentDir, "WuWaVH_HanViet_99_P.pak");
                var progress = new Progress<double>(p =>
                {
                    UpdateProgress = p;
                    UpdateStatusMessage = $"Đang tải PAK Hán Việt: {p:F0}%";
                });
                var rHv = await _update.DownloadFileAsync(pakHvUrl, CurrentUpdateManifest.PakHanVietSha256, destHv, progress);
                if (!rHv.Success)
                {
                    throw new Exception("Lỗi tải bản dịch Hán Việt: " + rHv.Error);
                }
            }

            // 3. Nếu game đã cài Việt Hóa, cập nhật luôn vào thư mục game
            var gamePath = _settings.Settings.GamePath;
            if (!string.IsNullOrWhiteSpace(gamePath) && _vietHoa != null)
            {
                var status = _vietHoa.GetStatus(gamePath);
                if (status.Installed)
                {
                    UpdateStatusMessage = "Đang cập nhật tệp Việt hóa vào thư mục game...";
                    var modDir = Path.Combine(gamePath, "Client", "Content", "Paks", "~WuWaMods");
                    if (Directory.Exists(modDir))
                    {
                        if (status.Variant.Equals("hanviet", StringComparison.OrdinalIgnoreCase))
                        {
                            var src = Path.Combine(contentDir, "WuWaVH_HanViet_99_P.pak");
                            if (File.Exists(src)) File.Copy(src, Path.Combine(modDir, "WuWaVH_HanViet_99_P.pak"), overwrite: true);
                        }
                        else
                        {
                            var src = Path.Combine(contentDir, "WuWaVH_EN_99_P.pak");
                            if (File.Exists(src)) File.Copy(src, Path.Combine(modDir, "WuWaVH_EN_99_P.pak"), overwrite: true);
                        }
                    }
                }
            }

            UpdateStatusMessage = "🎉 Cập nhật nhanh tệp Việt hóa thành công!";
            UpdateProgress = 100;
            HasUpdate = false;
            UpdateStatus = $"Đã cập nhật dữ liệu v{CurrentUpdateManifest.Version}";
            _settings.Settings.LastUpdateCheck = DateTimeOffset.UtcNow;
            _settings.Save();

            System.Windows.MessageBox.Show(
                $"Đã cập nhật nhanh tệp Việt hóa lên bản v{CurrentUpdateManifest.Version} thành công!\n\n" +
                "Nội dung bản dịch mới đã được nạp trực tiếp vào ứng dụng và thư mục game của bạn.",
                "VHWuWa — Cập Nhật Nhanh",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _log.Error("Update", "Lỗi cập nhật nhanh: " + ex.Message, ex);
            UpdateStatusMessage = "Lỗi cập nhật nhanh: " + ex.Message;
            System.Windows.MessageBox.Show("Cập nhật nhanh thất bại:\n" + ex.Message, "VHWuWa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsUpdating = false;
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
        UpdateStatusMessage = "Đang kết nối và tải bản cập nhật...";
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
                System.Windows.MessageBox.Show("Tải bản cập nhật thất bại:\n" + dlRes.Error, "VHWuWa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            UpdateStatusMessage = "Đang giải nén và thay thế tệp ứng dụng...";
            var zipPath = dlRes.Value;
            var appDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
            var pid = Environment.ProcessId;

            var scriptPath = Path.Combine(tempDir, "apply_update.ps1");
            var script = """
                Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase, System.Windows.Forms
                param([string]$Zip,[string]$Target,[int]$AppPid)
                $ErrorActionPreference = 'Stop'

                [xml]$xaml = @"
                <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                        Title="VHWuWa - Cập nhật tự động"
                        Height="210" Width="460"
                        WindowStartupLocation="CenterScreen"
                        WindowStyle="None" AllowsTransparency="True" Background="Transparent"
                        Topmost="True" ShowInTaskbar="True">
                    <Border Background="#181825" CornerRadius="16" BorderBrush="#7C3AED" BorderThickness="2" Padding="24">
                        <Border.Effect>
                            <DropShadowEffect BlurRadius="25" ShadowDepth="0" Color="#7C3AED" Opacity="0.45"/>
                        </Border.Effect>
                        <Grid>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="Auto"/>
                                <RowDefinition Height="*"/>
                                <RowDefinition Height="Auto"/>
                            </Grid.RowDefinitions>
                            <StackPanel Grid.Row="0" Orientation="Horizontal" VerticalAlignment="Center">
                                <TextBlock Text="⚡" FontSize="20" Margin="0,0,10,0"/>
                                <TextBlock Text="VHWuWa — Đang Tự Động Cập Nhật" Foreground="#F5E0DC" FontSize="16" FontWeight="Bold"/>
                            </StackPanel>
                            <TextBlock Name="StatusText" Grid.Row="1" Text="Đang chuẩn bị gói cập nhật..." 
                                       Foreground="#BAC2DE" FontSize="13" VerticalAlignment="Center" TextWrapping="Wrap"/>
                            <ProgressBar Name="ProgBar" Grid.Row="2" IsIndeterminate="True" Height="8" 
                                         Foreground="#CBA6F7" Background="#313244" BorderThickness="0"/>
                        </Grid>
                    </Border>
                </Window>
                "@

                $reader = [System.Xml.XmlNodeReader]::new($xaml)
                $window = [System.Windows.Markup.XamlReader]::Load($reader)
                $statusText = $window.FindName("StatusText")

                function Update-UI([string]$msg) {
                    if ($statusText) {
                        $statusText.Text = $msg
                        [System.Windows.Forms.Application]::DoEvents()
                    }
                    Write-Host $msg
                }

                $window.Show()
                [System.Windows.Forms.Application]::DoEvents()

                try {
                    if ($AppPid -gt 0) {
                        Update-UI '[1/4] Đang đóng tiến trình cũ...'
                        $p = Get-Process -Id $AppPid -ErrorAction SilentlyContinue
                        if ($p) {
                            $p.WaitForExit(5000) | Out-Null
                            if (-not $p.HasExited) {
                                Stop-Process -Id $AppPid -Force -ErrorAction SilentlyContinue
                                Start-Sleep -Milliseconds 500
                            }
                        }
                    }

                    $stage = Join-Path $env:TEMP ('VHWuWa_Extract_' + [guid]::NewGuid().ToString('N'))
                    $backup = $Target.TrimEnd('\\','/') + '_backup_' + (Get-Date -Format 'yyyyMMdd_HHmmss')

                    Update-UI '[2/4] Đang giải nén gói cập nhật...'
                    Expand-Archive -LiteralPath $Zip -DestinationPath $stage -Force

                    $exe = Get-ChildItem -LiteralPath $stage -Recurse -Filter 'VHWuWa.exe' -File |
                           Where-Object { $_.Directory.Name -ieq 'app' } |
                           Sort-Object { $_.FullName.Length } |
                           Select-Object -First 1
                    if (-not $exe) {
                        $exe = Get-Item -LiteralPath (Join-Path $stage 'VHWuWa.exe') -ErrorAction SilentlyContinue
                    }
                    if (-not $exe) {
                        $exe = Get-ChildItem -LiteralPath $stage -Recurse -Filter 'VHWuWa.exe' -File | Select-Object -First 1
                    }
                    if (-not $exe) { throw 'ZIP cập nhật không chứa file VHWuWa.exe hợp lệ!' }

                    $srcDir = $exe.Directory.FullName
                    Update-UI '[3/4] Đang sao lưu và cập nhật tệp ứng dụng...'
                    New-Item -ItemType Directory -Path $backup -Force | Out-Null
                    Get-ChildItem -LiteralPath $Target -Force | Copy-Item -Destination $backup -Recurse -Force

                    # Sao chép tệp mới với vòng lặp thử lại nếu bị khóa
                    $retryMax = 5
                    $success = $false
                    for ($i = 1; $i -le $retryMax; $i++) {
                        try {
                            Get-ChildItem -LiteralPath $srcDir -Force | Copy-Item -Destination $Target -Recurse -Force
                            $success = $true
                            break
                        } catch {
                            Update-UI "   -> Đang thử lại chép file (lần $i/$retryMax)..."
                            Start-Sleep -Milliseconds 600
                        }
                    }
                    if (-not $success) { throw 'Không thể ghi đè các tệp ứng dụng (tệp đang bị khóa).' }

                    if (-not (Test-Path -LiteralPath (Join-Path $Target 'VHWuWa.exe'))) {
                        throw 'Thiếu VHWuWa.exe sau khi cập nhật.'
                    }

                    Update-UI '[4/4] Khởi động lại ứng dụng mới...'
                    Remove-Item -LiteralPath (Join-Path $Target 'VHWuWa.Updater.exe') -Force -ErrorAction SilentlyContinue
                    Remove-Item -LiteralPath (Join-Path $Target 'VHWuWa.Updater.next.exe') -Force -ErrorAction SilentlyContinue
                    
                    $window.Close()
                    Start-Process -FilePath (Join-Path $Target 'VHWuWa.exe') -WorkingDirectory $Target
                    Remove-Item -LiteralPath $backup -Recurse -Force -ErrorAction SilentlyContinue
                    Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
                } catch {
                    if ($window) { $window.Close() }
                    Write-Host "LỖI CẬP NHẬT: $($_.Exception.Message)" -ForegroundColor Red
                    if (Test-Path -LiteralPath $backup) {
                        Write-Host 'Đang khôi phục lại phiên bản cũ...' -ForegroundColor Yellow
                        Get-ChildItem -LiteralPath $backup -Force | Copy-Item -Destination $Target -Recurse -Force
                        if (Test-Path -LiteralPath (Join-Path $Target 'VHWuWa.exe')) {
                            Start-Process -FilePath (Join-Path $Target 'VHWuWa.exe') -WorkingDirectory $Target
                        }
                    }
                    [System.Windows.Forms.MessageBox]::Show("Tự động cập nhật thất bại:`n$($_.Exception.Message)", "VHWuWa", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error)
                    throw
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
            System.Windows.MessageBox.Show("Lỗi tự động cập nhật:\n" + ex.Message, "VHWuWa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsUpdating = false;
        }
    }
}
