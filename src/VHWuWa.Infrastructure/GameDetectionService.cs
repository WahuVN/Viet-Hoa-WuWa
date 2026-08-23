using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.Infrastructure;

public sealed class GameDetectionService : IGameDetectionService
{
    private readonly ILogService _log;
    private readonly IReadOnlyList<string>? _searchRoots;
    public GameConfig GameConfig { get; }

    public GameDetectionService(ILogService log, string? configDir = null,
        IEnumerable<string>? searchRoots = null)
    {
        _log = log;
        _searchRoots = searchRoots?.Where(Directory.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var dir = configDir ?? AppPaths.DefaultConfigDir;
        var path = Path.Combine(dir, "game.json");
        try
        {
            GameConfig = File.Exists(path)
                ? VhwJson.Deserialize<GameConfig>(File.ReadAllText(path)) ?? new GameConfig()
                : new GameConfig();
        }
        catch (Exception ex)
        {
            _log.Error("GameDetection", $"Không đọc được game.json: {ex.Message}", ex);
            GameConfig = new GameConfig();
        }
    }

    public GameValidation Validate(string gamePath)
    {
        var r = new GameValidation { GamePath = gamePath };
        if (string.IsNullOrWhiteSpace(gamePath) || !Directory.Exists(gamePath))
        {
            r.Message = "Thư mục game không tồn tại.";
            return r;
        }
        if (GameConfig.RequiredFiles.Count == 0)
        {
            r.Message = "Chưa cấu hình danh sách file bắt buộc (Config/game.json).";
            return r;
        }
        foreach (var rf in GameConfig.RequiredFiles)
        {
            var full = Path.Combine(gamePath, rf.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(full)) r.MissingFiles.Add(rf);
        }
        r.IsValid = r.MissingFiles.Count == 0;
        r.DetectedVersion = r.IsValid ? DetectVersion(gamePath) : null;
        if (r.IsValid)
        {
            r.Message = "Đường dẫn game hợp lệ.";
        }
        else if (File.Exists(Path.Combine(gamePath, "Binaries", "Win64", "Client-Win64-Shipping.exe")))
        {
            var parent = Directory.GetParent(gamePath)?.FullName ?? "";
            r.Message = $"Bạn đang chọn vào thư mục 'Client'. Hãy chọn thư mục game cấp ngoài: '{parent}' (ví dụ: D:\\Game\\Wuthering Waves Game).";
        }
        else
        {
            r.Message = "Không tìm thấy file game. Hãy chọn thư mục 'Wuthering Waves Game' (thư mục chứa 'Client', ví dụ: D:\\Game\\Wuthering Waves Game).";
        }
        return r;
    }

    public string? DetectVersion(string gamePath)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(GameConfig.Executable))
            {
                var exe = Path.Combine(gamePath, GameConfig.Executable);
                if (File.Exists(exe))
                {
                    var v = FileVersionInfo.GetVersionInfo(exe).FileVersion;
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }
            // fallback: version.txt / *.version
            var vf = Path.Combine(gamePath, "version.txt");
            if (File.Exists(vf)) return File.ReadAllText(vf).Trim();
        }
        catch { }
        return null;
    }

    public string? NormalizeGamePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            var cleaned = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (File.Exists(cleaned)) cleaned = Path.GetDirectoryName(cleaned) ?? cleaned;
            cleaned = Path.GetFullPath(cleaned);

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = new DirectoryInfo(cleaned);
            for (var level = 0; current is not null && level < 8; level++, current = current.Parent)
            {
                foreach (var candidate in CandidateRoots(current.FullName))
                {
                    var normalized = candidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    if (!visited.Add(normalized) || !Directory.Exists(normalized)) continue;
                    if (Validate(normalized).IsValid) return normalized;
                }
            }
        }
        catch (Exception ex)
        {
            _log.Warn("GameDetection", $"Không chuẩn hóa được đường dẫn '{path}': {ex.Message}");
        }
        return null;
    }

    private static IEnumerable<string> CandidateRoots(string root)
    {
        yield return root;
        yield return Path.Combine(root, "Wuthering Waves Game");
        yield return Path.Combine(root, "Wuthering Waves");
        yield return Path.Combine(root, "Wuthering Waves", "Wuthering Waves Game");
    }

    public IReadOnlyList<string> AutoDetect()
    {
        var found = new List<string>();
        void TryAdd(string? p)
        {
            if (string.IsNullOrWhiteSpace(p)) return;
            var normalized = NormalizeGamePath(p);
            if (normalized is null) return;
            if (found.Any(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase))) return;
            found.Add(normalized);
        }

        // 1) Game đang chạy: nhận cả đường dẫn EXE nằm sâu trong Client\Binaries\Win64.
        if (OperatingSystem.IsWindows())
        {
            foreach (var processName in new[] { "Client-Win64-Shipping", "Wuthering Waves" })
            {
                try
                {
                    foreach (var process in Process.GetProcessesByName(processName))
                    {
                        using (process)
                        {
                            try { TryAdd(process.MainModule?.FileName); } catch { }
                        }
                    }
                }
                catch { }
            }
        }

        // 2) Registry (Windows). NormalizeGamePath xử lý cả thư mục launcher/thư mục cha.
        if (OperatingSystem.IsWindows())
        {
            foreach (var key in GameConfig.PossibleRegistryKeys)
                TryAdd(ReadRegistryInstallLocation(key));
        }

        // 3) Steam libraries: <lib>/steamapps/common/*
        foreach (var lib in SteamLibraries())
        {
            var common = Path.Combine(lib, "steamapps", "common");
            if (!Directory.Exists(common)) continue;
            TryAdd(common);
            foreach (var sub in SafeDirs(common))
                TryAdd(sub);
        }

        // 4) Các ổ đĩa/thư mục cài tùy chọn. Duyệt một tầng ở gốc để bắt được
        // D:\Game\Wuthering Waves Game, E:\Kuro Games\Wuthering Waves\Wuthering Waves Game, ...
        var names = new[]
        {
            GameConfig.GameName, GameConfig.GameId,
            "Wuthering Waves Game", "Wuthering Waves"
        }.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct();
        var roots = _searchRoots ?? DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
            .Select(d => d.RootDirectory.FullName).ToArray();
        foreach (var root in roots)
        {
            TryAdd(root);
            foreach (var name in names)
            {
                TryAdd(Path.Combine(root, name));
                foreach (var container in new[] { "Game", "Games", "Kuro Games", "Program Files", "Program Files (x86)" })
                    TryAdd(Path.Combine(root, container, name));
            }
            foreach (var topLevel in SafeDirs(root)) TryAdd(topLevel);
        }
        return found;
    }

    [SupportedOSPlatform("windows")]
    private static string ReadRegistryInstallLocation(string keyPath)
    {
        try
        {
            // keyPath dạng HKLM\SOFTWARE\... ; đọc value InstallLocation
            foreach (var root in new[] { Registry.LocalMachine, Registry.CurrentUser })
            {
                var sub = keyPath;
                if (sub.StartsWith("HKLM\\", StringComparison.OrdinalIgnoreCase)) sub = sub[5..];
                else if (sub.StartsWith("HKCU\\", StringComparison.OrdinalIgnoreCase)) sub = sub[5..];
                using var k = root.OpenSubKey(sub);
                var loc = k?.GetValue("InstallLocation") as string;
                if (!string.IsNullOrWhiteSpace(loc)) return loc!;
            }
        }
        catch { }
        return "";
    }

    private static IEnumerable<string> SteamLibraries()
    {
        var libs = new List<string>();
        string? steam = null;
        if (OperatingSystem.IsWindows())
        {
            try
            {
                using var k = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Valve\Steam");
                steam = k?.GetValue("SteamPath") as string;
            }
            catch { }
        }
        steam ??= @"C:\Program Files (x86)\Steam";
        if (!Directory.Exists(steam)) return libs;
        libs.Add(steam);
        var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdf))
        {
            try
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s*\"([^\"]+)\""))
                {
                    var p = m.Groups[1].Value.Replace("\\\\", "\\");
                    if (Directory.Exists(p)) libs.Add(p);
                }
            }
            catch { }
        }
        return libs.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SafeDirs(string root)
    {
        try { return Directory.GetDirectories(root); }
        catch { return Array.Empty<string>(); }
    }
}
