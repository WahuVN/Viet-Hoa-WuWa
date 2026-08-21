using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VHWuWa.Core.Abstractions;

namespace VHWuWa.App.ViewModels;

public partial class GraphicsOptionVm : ObservableObject
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public ObservableCollection<string> Choices { get; } = new();
    [ObservableProperty] private string _selected = "";
}

public partial class GraphicsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IGraphicsService _graphics;

    [ObservableProperty] private bool _isSupported;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _lockStatus = "";

    public ObservableCollection<string> Presets { get; } = new();
    public ObservableCollection<GraphicsOptionVm> Options { get; } = new();

    public GraphicsViewModel(ISettingsService settings, IGraphicsService graphics)
    {
        _settings = settings; _graphics = graphics;
    }

    public void OnActivated()
    {
        IsSupported = _graphics.IsSupported;
        if (!IsSupported)
        {
            Message = "Tính năng này chưa được cấu hình cho game hiện tại.";
            return;
        }
        Presets.Clear();
        foreach (var p in _graphics.Config.Presets) Presets.Add(p.Name);

        Options.Clear();
        var current = ReadCurrentSafe();
        foreach (var opt in _graphics.Config.Options)
        {
            var vm = new GraphicsOptionVm { Key = opt.Key, Label = opt.Label };
            foreach (var c in opt.Choices) vm.Choices.Add(c);
            vm.Selected = current.TryGetValue(opt.Key, out var v) ? v
                : (opt.Choices.FirstOrDefault() ?? "");
            Options.Add(vm);
        }
        Message = "Đọc cấu hình hiện tại xong.";
        RefreshLock();
    }

    private void RefreshLock()
    {
        try
        {
            LockStatus = _graphics.IsReadOnly(_settings.Settings.GamePath)
                ? "🔒 Engine.ini đang KHÓA — game không ghi đè được (tinh chỉnh được giữ)."
                : "🔓 Engine.ini chưa khóa — game có thể ghi đè khi khởi động.";
        }
        catch { LockStatus = ""; }
    }

    [RelayCommand]
    private void Lock()
    {
        var r = _graphics.SetReadOnly(_settings.Settings.GamePath, true);
        Message = r.Success ? "Đã khóa Engine.ini (chống game ghi đè)." : "Lỗi: " + r.Error;
        RefreshLock();
    }

    [RelayCommand]
    private void Unlock()
    {
        var r = _graphics.SetReadOnly(_settings.Settings.GamePath, false);
        Message = r.Success ? "Đã mở khóa Engine.ini." : "Lỗi: " + r.Error;
        RefreshLock();
    }

    private Dictionary<string, string> ReadCurrentSafe()
    {
        try { return _graphics.ReadCurrent(_settings.Settings.GamePath); }
        catch { return new(); }
    }

    [RelayCommand]
    private void ApplyPreset(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        var r = _graphics.ApplyPreset(_settings.Settings.GamePath, name);
        Message = r.Success ? $"Đã áp preset '{name}'." : "Lỗi: " + r.Error;
        OnActivated();
    }

    [RelayCommand]
    private void ApplyCustom()
    {
        var values = Options.ToDictionary(o => o.Key, o => o.Selected);
        var r = _graphics.Apply(_settings.Settings.GamePath, values);
        Message = r.Success ? "Đã áp cấu hình tùy chỉnh." : "Lỗi: " + r.Error;
        RefreshLock();
    }

    [RelayCommand]
    private void ReadCurrent() => OnActivated();

    [RelayCommand]
    private void OpenConfig()
    {
        var file = _graphics.ConfigFilePath(_settings.Settings.GamePath);
        if (file is not null && File.Exists(file))
            Process.Start(new ProcessStartInfo(file) { UseShellExecute = true });
        else Message = "Chưa có file cấu hình để mở.";
    }
}
