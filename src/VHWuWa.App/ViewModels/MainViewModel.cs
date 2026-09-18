using System.Diagnostics;
using System.IO;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Core.Services;
using Wpf.Ui.Appearance;

namespace VHWuWa.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string DiscordUrl = "https://discord.gg/Gy5YQ84Yc2";
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
        + (Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0")
        + " · game 3.6";

    public MainViewModel(ISettingsService settings, IGameDetectionService detect, IUpdateService update, ILogService log, IViethoaInstaller? vietHoa = null)
    {
        _settings = settings;
        _detect = detect;
        _update = update;
        _log = log;
        _vietHoa = vietHoa;
        _appVersion = "VHWuWa v" + (Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0");
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
                CurrentUpdateManifest = null;
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

                // AutoCheckUpdate vẫn phải báo khi thật sự có bản mới; "silent" chỉ
                // ẩn lỗi mạng và thông báo "đã mới nhất" lúc khởi động.
                var promptMsg = $"Đã tìm thấy bản cập nhật mới v{r.Manifest.Version}!\n\n"
                                + $"Ghi chú:\n{UpdateNotes}\n\n"
                                + "Bạn có muốn tự động tải và cài đặt bản cập nhật ngay bây giờ không?";
                var ask = System.Windows.MessageBox.Show(
                    promptMsg,
                    "VHWuWa — Cập Nhật",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);

                if (ask == System.Windows.MessageBoxResult.Yes)
                    _ = ApplyUpdateAsync();
            }
            else
            {
                CurrentUpdateManifest = null;
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
                    throw new Exception("Lỗi tải bản dịch EN: " + rEn.Error);
            }

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
                    throw new Exception("Lỗi tải bản dịch Hán Việt: " + rHv.Error);
            }

            var gamePath = _settings.Settings.GamePath;
            if (!string.IsNullOrWhiteSpace(gamePath) && _vietHoa != null)
            {
                var status = _vietHoa.GetStatus(gamePath);
                if (status.Installed)
                {
                    UpdateStatusMessage = "Đang cập nhật tệp Việt hóa vào thư mục game...";
                    var variant = status.Variant.Equals("hanviet", StringComparison.OrdinalIgnoreCase)
                        ? NameVariant.HanViet
                        : NameVariant.English;
                    var applyPak = await _vietHoa.UpdateTranslationPakAsync(gamePath, variant);
                    if (!applyPak.Success)
                        throw new Exception("Không thể cập nhật PAK đang hoạt động trong game: " + applyPak.Error);
                }
            }

            UpdateStatusMessage = "🎉 Cập nhật nhanh tệp Việt hóa thành công!";
            UpdateProgress = 100;
            // Chỉ cập nhật PAK không làm thay đổi phiên bản VHWuWa.exe. Nếu release
            // cũng có app mới thì vẫn phải giữ nút cập nhật đầy đủ hiển thị để người
            // dùng không bị kẹt ở app cũ sau khi đã cập nhật dữ liệu bản dịch.
            HasUpdate = VersionComparer.IsNewer(
                CurrentUpdateManifest.Version,
                Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0");
            UpdateStatus = HasUpdate
                ? $"Đã cập nhật dữ liệu v{CurrentUpdateManifest.Version} · còn bản app mới"
                : $"Đã cập nhật dữ liệu v{CurrentUpdateManifest.Version}";
            _settings.Settings.LastUpdateCheck = DateTimeOffset.UtcNow;
            _settings.Save();

            System.Windows.MessageBox.Show(
                $"Đã cập nhật nhanh tệp Việt hóa lên bản v{CurrentUpdateManifest.Version} thành công!\n\n"
                + "Nội dung bản dịch mới đã được nạp trực tiếp vào ứng dụng và thư mục game của bạn.",
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
        if (IsUpdating || CurrentUpdateManifest is null) return;
        var manifest = CurrentUpdateManifest;
        if (string.IsNullOrWhiteSpace(manifest.DownloadUrl)
            || string.IsNullOrWhiteSpace(manifest.Version)
            || string.IsNullOrWhiteSpace(manifest.Sha256))
        {
            UpdateStatusMessage = "Release thiếu URL, version hoặc SHA-256 nên không thể tự cập nhật an toàn.";
            return;
        }

        var appDirectory = AppContext.BaseDirectory.TrimEnd(
            Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var bundledUpdater = Path.Combine(appDirectory, UpdateArchiveInstaller.UpdaterExecutable);
        if (!File.Exists(bundledUpdater))
        {
            UpdateStatusMessage = "Bản hiện tại thiếu VHWuWa.Updater.exe; không thể tự cập nhật.";
            System.Windows.MessageBox.Show(
                UpdateStatusMessage,
                "VHWuWa — Lỗi Cập Nhật",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return;
        }

        IsUpdating = true;
        UpdateProgress = 0;
        var tempDirectory = Path.Combine(
            Path.GetTempPath(), "VHWuWa_Update_" + Guid.NewGuid().ToString("N"));
        var handoffStarted = false;

        try
        {
            UpdateStatusMessage = "Đang kết nối và tải bản cập nhật...";
            var progress = new Progress<double>(value =>
            {
                UpdateProgress = value;
                UpdateStatusMessage = $"Đang tải bản cập nhật: {value:F0}%";
            });
            var download = await _update.DownloadAsync(manifest, tempDirectory, progress);
            if (!download.Success || string.IsNullOrWhiteSpace(download.Value))
                throw new InvalidOperationException(download.Error ?? "Không tải được ZIP cập nhật.");

            UpdateStatusMessage = "Đã xác minh gói; đang chuyển sang trình cập nhật an toàn...";
            var updaterCopy = Path.Combine(tempDirectory, "VHWuWa.Updater.run.exe");
            var readyFile = Path.Combine(tempDirectory, "updater-ready.txt");
            File.Copy(bundledUpdater, updaterCopy, overwrite: true);

            using var currentProcess = Process.GetCurrentProcess();
            var startInfo = new ProcessStartInfo(updaterCopy)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = tempDirectory,
            };
            startInfo.ArgumentList.Add("--zip");
            startInfo.ArgumentList.Add(download.Value);
            startInfo.ArgumentList.Add("--target");
            startInfo.ArgumentList.Add(appDirectory);
            startInfo.ArgumentList.Add("--relaunch");
            startInfo.ArgumentList.Add(UpdateArchiveInstaller.MainExecutable);
            startInfo.ArgumentList.Add("--pid");
            startInfo.ArgumentList.Add(Environment.ProcessId.ToString());
            startInfo.ArgumentList.Add("--process-start-ticks");
            startInfo.ArgumentList.Add(currentProcess.StartTime.ToUniversalTime().Ticks.ToString());
            startInfo.ArgumentList.Add("--expected-version");
            startInfo.ArgumentList.Add(manifest.Version);
            startInfo.ArgumentList.Add("--sha256");
            startInfo.ArgumentList.Add(manifest.Sha256);
            startInfo.ArgumentList.Add("--cleanup-dir");
            startInfo.ArgumentList.Add(tempDirectory);
            startInfo.ArgumentList.Add("--ready-file");
            startInfo.ArgumentList.Add(readyFile);

            using var updaterProcess = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Không mở được VHWuWa.Updater.exe.");
            if (!await WaitForUpdaterReadyAsync(updaterProcess, readyFile, TimeSpan.FromSeconds(10)))
            {
                TryStopUpdater(updaterProcess);
                throw new InvalidOperationException(
                    "Updater không xác nhận sẵn sàng; ứng dụng hiện tại vẫn được giữ nguyên.");
            }
            handoffStarted = true;
            System.Windows.Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            _log.Error("Update", "Lỗi tự động cập nhật: " + ex.Message, ex);
            UpdateStatusMessage = "Lỗi tự động cập nhật: " + ex.Message;
            System.Windows.MessageBox.Show(
                "Lỗi tự động cập nhật:\n" + ex.Message,
                "VHWuWa",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            if (!handoffStarted)
                TryDeleteUpdateTemp(tempDirectory);
            IsUpdating = false;
        }
    }

    private static void TryDeleteUpdateTemp(string directory)
    {
        try
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Không che lỗi gốc chỉ vì Windows Defender còn giữ file tải trong chốc lát.
        }
    }

    private static async Task<bool> WaitForUpdaterReadyAsync(
        Process updater,
        string readyFile,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(readyFile))
            {
                try
                {
                    if (!string.Equals(
                            await File.ReadAllTextAsync(readyFile),
                            "ready",
                            StringComparison.Ordinal))
                        return false;
                    await Task.Delay(200);
                    return !updater.HasExited;
                }
                catch (IOException)
                {
                    // Updater có thể vừa tạo file; thử lại.
                }
            }
            if (updater.HasExited) return false;
            await Task.Delay(100);
        }
        return false;
    }

    private static void TryStopUpdater(Process updater)
    {
        try
        {
            if (!updater.HasExited)
            {
                updater.Kill(entireProcessTree: true);
                updater.WaitForExit(5_000);
            }
        }
        catch
        {
            // Ứng dụng chính chưa shutdown, nên bản đang dùng vẫn an toàn.
        }
    }
}
