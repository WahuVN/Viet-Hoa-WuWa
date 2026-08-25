using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;

namespace VHWuWa.App.ViewModels;

public partial class GuideViewModel : ObservableObject
{
    private readonly string _guidesDir = Path.Combine(AppContext.BaseDirectory, "Guides", "vi-VN");
    private List<GuideEntry> _all = new();

    private static readonly Dictionary<string, (string Title, string Icon, string Summary)> Metadata =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["01-bat-dau.md"] = ("Bắt đầu nhanh", "🚀", "Cài và mở game trong vài bước"),
            ["02-chon-thu-muc-game.md"] = ("Chọn thư mục game", "📁", "Tự dò hoặc chọn đúng thư mục Client"),
            ["03-cai-viet-hoa.md"] = ("Cài Việt hóa", "🌐", "Chọn bản tên và cài an toàn"),
            ["04-go-viet-hoa.md"] = ("Gỡ Việt hóa", "🧹", "Trả game về trạng thái sạch"),
            ["05-cai-go-mod.md"] = ("Xử lý xung đột", "🛡", "Kiểm tra và dọn mod cũ"),
            ["06-doi-font.md"] = ("Font chữ", "🔤", "Chọn, xem thử và khôi phục font"),
            ["07-do-hoa.md"] = ("Đồ họa", "🎮", "Preset, tùy chỉnh và sao lưu"),
            ["08-loi-thuong-gap.md"] = ("Khắc phục lỗi", "🧰", "Các bước kiểm tra nhanh"),
            ["09-dong-gop-ban-dich.md"] = ("Đóng góp bản dịch", "✍", "Quy trình sửa và gửi nội dung"),
        };

    [ObservableProperty] private string _search = "";
    [ObservableProperty] private GuideEntry? _selected;

    public ObservableCollection<GuideEntry> Guides { get; } = new();
    public ObservableCollection<GuideBlock> Blocks { get; } = new();

    public void OnActivated()
    {
        _all = Directory.Exists(_guidesDir)
            ? Directory.GetFiles(_guidesDir, "*.md")
                .Select(Path.GetFileName).OfType<string>()
                .OrderBy(name => name)
                .Select(CreateEntry).ToList()
            : new();
        ApplyFilter();
        if (Selected is null && Guides.Count > 0) Selected = Guides[0];
    }

    private static GuideEntry CreateEntry(string fileName)
    {
        var fallback = Path.GetFileNameWithoutExtension(fileName).Replace('-', ' ');
        return Metadata.TryGetValue(fileName, out var meta)
            ? new GuideEntry(fileName, meta.Title, meta.Icon, meta.Summary)
            : new GuideEntry(fileName, fallback, "📄", "Tài liệu hướng dẫn");
    }

    partial void OnSearchChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Guides.Clear();
        foreach (var g in _all)
            if (string.IsNullOrWhiteSpace(Search)
                || g.Title.Contains(Search, StringComparison.OrdinalIgnoreCase)
                || g.Summary.Contains(Search, StringComparison.OrdinalIgnoreCase))
                Guides.Add(g);
        if (Selected is not null && !Guides.Any(item => item.FileName == Selected.FileName))
            Selected = Guides.FirstOrDefault();
    }

    partial void OnSelectedChanged(GuideEntry? value)
    {
        Blocks.Clear();
        if (value is null) return;
        try
        {
            var path = Path.Combine(_guidesDir, value.FileName);
            if (!File.Exists(path)) { Blocks.Add(new GuideBlock("p", "(Không đọc được nội dung.)")); return; }
            foreach (var b in ParseMarkdown(File.ReadAllText(path, Encoding.UTF8))) Blocks.Add(b);
        }
        catch (Exception ex) { Blocks.Add(new GuideBlock("p", "Lỗi đọc hướng dẫn: " + ex.Message)); }
    }

    private static string Clean(string s)
    {
        s = s.Replace("**", "").Replace("`", "");
        // [text](url) -> text
        int i;
        while ((i = s.IndexOf('[')) >= 0)
        {
            int j = s.IndexOf(']', i);
            if (j < 0) break;
            int k = (j + 1 < s.Length && s[j + 1] == '(') ? s.IndexOf(')', j) : -1;
            var text = s.Substring(i + 1, j - i - 1);
            if (k > 0) s = s.Remove(i, k - i + 1).Insert(i, text);
            else s = s.Remove(j, 1).Remove(i, 1);
        }
        return s.Trim();
    }

    private static IEnumerable<GuideBlock> ParseMarkdown(string md)
    {
        var inCode = false;
        foreach (var raw in md.Replace("\r", "").Split('\n'))
        {
            var line = raw.TrimEnd();
            var t = line.TrimStart();
            if (t.StartsWith("```")) { inCode = !inCode; continue; }
            if (inCode) { yield return new GuideBlock("code", line); continue; }
            if (t.Length == 0) { yield return new GuideBlock("space", ""); continue; }
            if (t.StartsWith("### ")) { yield return new GuideBlock("h3", Clean(t[4..])); continue; }
            if (t.StartsWith("## ")) { yield return new GuideBlock("h2", Clean(t[3..])); continue; }
            if (t.StartsWith("# ")) { yield return new GuideBlock("h1", Clean(t[2..])); continue; }
            if (t.StartsWith("- ") || t.StartsWith("* ")) { yield return new GuideBlock("li", "•  " + Clean(t[2..])); continue; }
            if (System.Text.RegularExpressions.Regex.IsMatch(t, @"^\d+[.)]\s+"))
            { yield return new GuideBlock("step", Clean(t)); continue; }
            if (t.StartsWith("> ")) { yield return new GuideBlock("note", Clean(t[2..])); continue; }
            if (t.StartsWith("|")) { yield return new GuideBlock("p", Clean(t.Trim('|').Replace("|", "   "))); continue; }
            if (t.StartsWith("---")) { yield return new GuideBlock("space", ""); continue; }
            yield return new GuideBlock("p", Clean(t));
        }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        if (Directory.Exists(_guidesDir)) Process.Start(new ProcessStartInfo(_guidesDir) { UseShellExecute = true });
    }
}

public sealed record GuideEntry(string FileName, string Title, string Icon, string Summary);

public sealed record GuideBlock(string Kind, string Text);

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IUpdateService _update;
    private readonly ILogService _log;
    private readonly MainViewModel _main;

    [ObservableProperty] private string _gamePath = "";
    [ObservableProperty] private bool _isDark = true;
    [ObservableProperty] private bool _autoCheckUpdate = true;
    [ObservableProperty] private string _appVersion = "";
    [ObservableProperty] private string _updateMessage = "";
    [ObservableProperty] private bool _busy;

    public MainViewModel Main => _main;

    public SettingsViewModel(ISettingsService settings, IUpdateService update, ILogService log, MainViewModel main)
    {
        _settings = settings; _update = update; _log = log; _main = main;
        GamePath = settings.Settings.GamePath;
        IsDark = !settings.Settings.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        AutoCheckUpdate = settings.Settings.AutoCheckUpdate;
        AppVersion = _main.AppVersion;
    }

    public void OnActivated() => GamePath = _settings.Settings.GamePath;

    [RelayCommand]
    private void ChooseFolder()
    {
        var dlg = new OpenFolderDialog { Title = "Chọn thư mục game" };
        if (dlg.ShowDialog() != true) return;
        GamePath = dlg.FolderName;
        _settings.Settings.GamePath = GamePath;
        _settings.Save();
        _main.RefreshStatus();
    }

    partial void OnIsDarkChanged(bool value)
    {
        _settings.Settings.Theme = value ? "Dark" : "Light";
        _settings.Save();
        App.ApplyTheme(_settings.Settings.Theme);
        _main.IsDark = value;
    }

    partial void OnAutoCheckUpdateChanged(bool value)
    {
        _settings.Settings.AutoCheckUpdate = value;
        _settings.Save();
    }

    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        Busy = true;
        try
        {
            var r = await _main.CheckUpdateAsync(false);
            UpdateMessage = r.Message;
        }
        finally { Busy = false; }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        if (Directory.Exists(_log.LogDirectory))
            Process.Start(new ProcessStartInfo(_log.LogDirectory) { UseShellExecute = true });
    }
}
