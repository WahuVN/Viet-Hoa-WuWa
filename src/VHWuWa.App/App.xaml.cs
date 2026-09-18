using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using VHWuWa.App.Services;
using VHWuWa.App.ViewModels;
using VHWuWa.App.Views;
using VHWuWa.Core.Abstractions;
using VHWuWa.Infrastructure;
using Wpf.Ui.Appearance;

namespace VHWuWa.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (GameExitWatchdog.IsWatchdogInvocation(e.Args))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var exitCode = GameExitWatchdog.Run(e.Args);
            Shutdown(exitCode);
            return;
        }

        var services = new ServiceCollection();
        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        services.AddVhwInfrastructure(configDir);

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<InstallViewModel>();
        services.AddSingleton<CharacterNamesViewModel>();
        services.AddSingleton<TermsEditorViewModel>();
        services.AddSingleton<UidEditorViewModel>();
        services.AddSingleton<FontViewModel>();
        services.AddSingleton<GraphicsViewModel>();
        services.AddSingleton<GuideViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<PakEditorBridge>();

        // Pages
        services.AddSingleton<HomePage>();
        services.AddSingleton<CharacterNamesPage>();
        services.AddSingleton<TermsEditorPage>();
        services.AddSingleton<UidEditorPage>();
        services.AddSingleton<FontPage>();
        services.AddSingleton<GraphicsPage>();
        services.AddSingleton<GuidePage>();
        services.AddSingleton<SettingsPage>();

        services.AddSingleton<MainWindow>();
        Services = services.BuildServiceProvider();

        // Bắt lỗi UI chưa xử lý (không crash im lặng)
        DispatcherUnhandledException += OnUnhandledException;

        ApplyTheme(Services.GetRequiredService<ISettingsService>().Settings.Theme);
        var mainWindow = Services.GetRequiredService<MainWindow>();
        ApplyWindowIcon(mainWindow);
        mainWindow.Show();
        CompleteUpdateStartup(e.Args);
        EnsureDesktopShortcut();
    }

    public static void ApplyTheme(string theme)
    {
        var t = theme.Equals("Light", StringComparison.OrdinalIgnoreCase)
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;
        ApplicationThemeManager.Apply(t);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show("Đã xảy ra lỗi không mong muốn:\n" + e.Exception.Message,
            "VHWuWa", MessageBoxButton.OK, MessageBoxImage.Error);
        try { Services.GetService<ILogService>()?.Error("UI", e.Exception.Message, e.Exception); } catch { }
        e.Handled = true;
    }

    private static void ApplyWindowIcon(Window window)
    {
        // FluentWindow đôi lúc không nạp URI ICO từ XAML khi chạy single-file,
        // khiến icon taskbar/Task Manager rơi về biểu tượng WPF trắng. Gán frame
        // đã cache trực tiếp lúc startup để Windows nhận đúng HICON của cửa sổ.
        var iconUri = new Uri("pack://application:,,,/Assets/app-logo-transparent.ico", UriKind.Absolute);
        window.Icon = BitmapFrame.Create(iconUri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
    }

    private static void CompleteUpdateStartup(string[] args)
    {
        try
        {
            var options = ParseArguments(args);
            var cleanupDirectory = ValidateUpdateCleanupDirectory(
                options.GetValueOrDefault("update-cleanup-dir"));

            if (options.TryGetValue("update-health-file", out var healthFile)
                && options.TryGetValue("update-expected-version", out var expectedVersion)
                && cleanupDirectory is not null)
            {
                var normalizedHealthFile = Path.GetFullPath(healthFile);
                if (!VHWuWa.Core.Services.PathValidation.IsSubPathOf(
                        normalizedHealthFile, cleanupDirectory))
                    throw new InvalidDataException("Đường dẫn health-check không hợp lệ.");

                var actualVersion = NormalizeVersion(
                    Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? string.Empty);
                if (!string.Equals(actualVersion, NormalizeVersion(expectedVersion),
                        StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        $"App khởi động sai phiên bản: cần {expectedVersion}, thực tế {actualVersion}.");

                Directory.CreateDirectory(cleanupDirectory);
                File.WriteAllText(normalizedHealthFile, actualVersion);
            }

            if (cleanupDirectory is not null)
            {
                var updaterPid = options.TryGetValue("update-updater-pid", out var pidText)
                                 && int.TryParse(pidText, out var parsedPid)
                    ? parsedPid
                    : 0;
                var updaterStartTicks = options.TryGetValue("update-updater-start-ticks", out var ticksText)
                                        && long.TryParse(ticksText, out var parsedTicks)
                    ? parsedTicks
                    : 0;
                _ = Task.Run(() => CleanupUpdateDirectory(
                    cleanupDirectory, updaterPid, updaterStartTicks));
            }
        }
        catch
        {
            // Không ghi health file: updater sẽ tự rollback. Không làm app crash thêm.
        }
    }

    private static void EnsureDesktopShortcut()
    {
        try
        {
            _ = DesktopShortcutService.EnsureCreated();
        }
        catch (Exception ex)
        {
            try
            {
                Services.GetService<ILogService>()?.Warn(
                    "Shortcut", "Không tạo được shortcut Desktop: " + ex.Message);
            }
            catch
            {
                // Shortcut chỉ là tiện ích; tuyệt đối không làm app khởi động thất bại.
            }
        }
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) continue;
            var key = args[i][2..];
            var value = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
            result[key] = value;
        }

        return result;
    }

    private static string? ValidateUpdateCleanupDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var cleanup = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var temp = Path.GetFullPath(Path.GetTempPath())
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (!string.Equals(Path.GetDirectoryName(cleanup), temp,
                StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(cleanup).StartsWith("VHWuWa_Update_",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Thư mục dọn cập nhật không hợp lệ.");

        return cleanup;
    }

    private static void CleanupUpdateDirectory(
        string directory,
        int updaterPid,
        long updaterStartTicks)
    {
        if (updaterPid > 0)
        {
            try
            {
                using var updater = Process.GetProcessById(updaterPid);
                if (updaterStartTicks <= 0
                    || updater.StartTime.ToUniversalTime().Ticks == updaterStartTicks)
                    updater.WaitForExit(120_000);
            }
            catch
            {
                // Updater đã thoát.
            }
        }

        for (var attempt = 0; attempt < 12; attempt++)
        {
            try
            {
                if (!Directory.Exists(directory)) return;
                foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);
                Directory.Delete(directory, recursive: true);
                return;
            }
            catch
            {
                Thread.Sleep(500);
            }
        }
    }

    private static string NormalizeVersion(string version)
    {
        if (!Version.TryParse(version, out var parsed)) return version.Trim();
        return new Version(parsed.Major, Math.Max(0, parsed.Minor), Math.Max(0, parsed.Build)).ToString();
    }
}
