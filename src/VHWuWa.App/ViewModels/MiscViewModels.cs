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
    private List<string> _all = new();

    [ObservableProperty] private string _search = "";
    [ObservableProperty] private string? _selected;

    public ObservableCollection<string> Guides { get; } = new();
    /// <summary>Các khối nội dung đã định dạng (tiêu đề / gạch đầu dòng / đoạn) để hiển thị rõ ràng.</summary>
    public ObservableCollection<GuideBlock> Blocks { get; } = new();

    public void OnActivated()
    {
        _all = Directory.Exists(_guidesDir)
            ? Directory.GetFiles(_guidesDir, "*.md").Select(Path.GetFileName).OfType<string>().OrderBy(x => x).ToList()
            : new();
        ApplyFilter();
        if (Selected is null && Guides.Count > 0) Selected = Guides[0];   // mở sẵn mục đầu
    }

    partial void OnSearchChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Guides.Clear();
        foreach (var g in _all)
            if (string.IsNullOrWhiteSpace(Search) || g.Contains(Search, StringComparison.OrdinalIgnoreCase))
                Guides.Add(g);
    }

    partial void OnSelectedChanged(string? value)
    {
        Blocks.Clear();
        if (string.IsNullOrWhiteSpace(value)) return;
        try
        {
            var path = Path.Combine(_guidesDir, value);
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
        foreach (var raw in md.Replace("\r", "").Split('\n'))
        {
            var line = raw.TrimEnd();
            var t = line.TrimStart();
            if (t.Length == 0) { yield return new GuideBlock("space", ""); continue; }
            if (t.StartsWith("### ")) { yield return new GuideBlock("h3", Clean(t[4..])); continue; }
            if (t.StartsWith("## ")) { yield return new GuideBlock("h2", Clean(t[3..])); continue; }
            if (t.StartsWith("# ")) { yield return new GuideBlock("h1", Clean(t[2..])); continue; }
            if (t.StartsWith("- ") || t.StartsWith("* ")) { yield return new GuideBlock("li", "•  " + Clean(t[2..])); continue; }
            if (t.Length > 2 && char.IsDigit(t[0]) && (t[1] == '.' || (t.Length > 2 && t[1] == ')' )))
            { yield return new GuideBlock("li", Clean(t)); continue; }
            if (t.StartsWith("> ")) { yield return new GuideBlock("quote", Clean(t[2..])); continue; }
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

/// <summary>Một khối nội dung hướng dẫn đã phân loại (h1/h2/h3/li/quote/p/space).</summary>
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
