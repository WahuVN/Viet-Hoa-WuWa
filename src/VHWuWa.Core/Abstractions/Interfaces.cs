using VHWuWa.Core.Models;

namespace VHWuWa.Core.Abstractions;

/// <summary>Ghi log (Serilog) + đọc log gần đây cho UI.</summary>
public interface ILogService
{
    void Info(string operation, string message);
    void Warn(string operation, string message);
    void Error(string operation, string message, Exception? ex = null);
    string LogDirectory { get; }
    IReadOnlyList<LogEntry> ReadRecent(int max = 500, string? levelFilter = null, string? search = null);
    void Clear();
}

/// <summary>Quản lý cấu hình ứng dụng + trạng thái cài đặt (LocalAppData).</summary>
public interface ISettingsService
{
    string AppDataDirectory { get; }
    string BackupsDirectory { get; }
    AppSettings Settings { get; }
    void Save();
    InstalledState LoadState();
    void SaveState(InstalledState state);
}

/// <summary>Nhận diện &amp; kiểm tra thư mục game (đọc Config/game.json).</summary>
public interface IGameDetectionService
{
    GameConfig GameConfig { get; }
    GameValidation Validate(string gamePath);
    /// <summary>Tự động dò thư mục game ở các vị trí phổ biến + Steam + Registry.</summary>
    IReadOnlyList<string> AutoDetect();
    string? DetectVersion(string gamePath);
}

/// <summary>Sao lưu &amp; khôi phục file gốc.</summary>
public interface IBackupService
{
    /// <summary>Tạo backup cho danh sách đích (tương đối game). Trả BackupManifest.</summary>
    BackupManifest CreateBackup(string gamePath, string operation, string packageId, string version,
        IEnumerable<string> destinations);
    Result Restore(string gamePath, string backupId);
    IReadOnlyList<BackupInfo> List();
    Result Delete(string backupId);
    string BackupsDirectory { get; }
}

/// <summary>Cài Việt hóa TRỰC TIẾP từ nội dung dựng sẵn (content\): chọn pak tên Hán Việt / EN,
/// kèm font + loader — sao chép vào Client\Content\Paks\~WuWaMods\, có backup version.dll.</summary>
public interface IViethoaInstaller
{
    /// <summary>Kiểm tra nội dung Việt hóa dựng sẵn đi kèm app.</summary>
    ViethoaContent InspectContent();
    /// <summary>Trạng thái đã cài trong thư mục game.</summary>
    ViethoaStatus GetStatus(string gamePath);
    /// <summary>Dò PAK/mod/loader khác có thể xung đột với bản Việt hóa tích hợp.</summary>
    IReadOnlyList<string> FindConflicts(string gamePath);
    /// <summary>Cài bản Việt hóa theo biến thể tên nhân vật.</summary>
    Task<Result> InstallAsync(string gamePath, NameVariant variant, bool withFont,
        IProgress<InstallProgress>? progress = null, CancellationToken ct = default);
    /// <summary>Gỡ Việt hóa và khôi phục file gốc.</summary>
    Task<Result> UninstallAsync(string gamePath, CancellationToken ct = default);
}

/// <summary>Cài / gỡ gói .vhwpack (Việt hóa hoặc mod) — có kiểm tra, backup, rollback.</summary>
public interface IPackageInstallerService
{
    Task<Result> InstallAsync(string gamePath, string vhwpackPath,
        IProgress<InstallProgress>? progress = null, CancellationToken ct = default);
    Task<Result> UninstallAsync(string gamePath, string packageId, CancellationToken ct = default);
    /// <summary>Xem trước manifest + kiểm tra chữ ký/hash (không cài).</summary>
    Task<Result<PackageManifest>> InspectAsync(string vhwpackPath, CancellationToken ct = default);
}

/// <summary>Quản lý mod: liệt kê, bật/tắt, phát hiện xung đột.</summary>
public interface IModService
{
    IReadOnlyList<ModInfo> ListInstalled(string gamePath);
    Result SetEnabled(string gamePath, string packageId, bool enabled);
    IReadOnlyList<string> DetectConflicts(string vhwpackPath);
}

/// <summary>Đổi font trong game (quản lý bằng manifest, có backup).</summary>
public interface IFontService
{
    Task<Result> ApplyFontAsync(string gamePath, string vhwpackPath, CancellationToken ct = default);
    Task<Result> RestoreDefaultAsync(string gamePath, CancellationToken ct = default);

    /// <summary>Áp 1 font pak thô (từ thư viện Fonts/) vào ~WuWaMods/. Yêu cầu đã cài Việt hóa.</summary>
    Task<Result> ApplyFontPakAsync(string gamePath, string fontPakPath, CancellationToken ct = default);

    /// <summary>Xóa mọi font pak trong ~WuWaMods/ → trả về font mặc định của bản VH.</summary>
    Task<Result> RemoveFontPaksAsync(string gamePath, CancellationToken ct = default);

    /// <summary>Tên file font pak đang dùng trong ~WuWaMods/ (hoặc null).</summary>
    string? CurrentFontPak(string gamePath);
}

/// <summary>Render ảnh xem trước một file font (.ttf/.otf/.ttc) với chữ mẫu tiếng Việt.</summary>
public interface IFontPreviewService
{
    /// <summary>Trả về PNG (byte[]) hoặc null nếu không đọc được font / không phải Windows.</summary>
    byte[]? RenderPreview(string fontFilePath, string sampleText, int fontSize = 30);
}

/// <summary>Chỉnh cấu hình đồ họa (đọc Config/graphics.json), có preset + backup.</summary>
public interface IGraphicsService
{
    GraphicsConfig Config { get; }
    bool IsSupported { get; }
    Dictionary<string, string> ReadCurrent(string gamePath);
    Result Apply(string gamePath, Dictionary<string, string> values);
    Result ApplyPreset(string gamePath, string presetName);
    string? ConfigFilePath(string gamePath);
    /// <summary>File cấu hình có đang ở chế độ chỉ-đọc (khóa chống game ghi đè) không.</summary>
    bool IsReadOnly(string gamePath);
    /// <summary>Đặt/bỏ chế độ chỉ-đọc cho file cấu hình.</summary>
    Result SetReadOnly(string gamePath, bool readOnly);
    /// <summary>Khôi phục Engine.ini về mặc định gốc của game và mở khóa.</summary>
    Result RestoreDefault(string gamePath);
}

/// <summary>Kiểm tra &amp; tải cập nhật ứng dụng từ GitHub Releases.</summary>
public interface IUpdateService
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default);
    Task<Result<string>> DownloadAsync(UpdateManifest manifest, string destDir,
        IProgress<double>? progress = null, CancellationToken ct = default);
}
