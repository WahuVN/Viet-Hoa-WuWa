using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public partial class ModViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IModService _mods;
    private readonly IPackageInstallerService _installer;
    private readonly IViethoaInstaller _viet;
    private readonly IFontService _fonts;

    [ObservableProperty] private ModInfo? _selectedMod;
    [ObservableProperty] private string _message = "";
    [ObservableProperty] private string _statusLine = "";
    [ObservableProperty] private bool _busy;

    public ObservableCollection<ModInfo> Mods { get; } = new();

    public ModViewModel(ISettingsService settings, IModService mods, IPackageInstallerService installer,
        IViethoaInstaller viet, IFontService fonts)
    {
        _settings = settings; _mods = mods; _installer = installer; _viet = viet; _fonts = fonts;
    }

    public void OnActivated() => Refresh();

    [RelayCommand]
    private void Refresh()
    {
        Mods.Clear();
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { Message = "Chưa chọn thư mục game (vào Trang chủ)."; StatusLine = ""; return; }
        foreach (var m in _mods.ListInstalled(path)) Mods.Add(m);
        Message = Mods.Count == 0 ? "Chưa có mod .vhwpack nào." : $"{Mods.Count} mod đã cài.";

        // Trạng thái Việt hóa hiện tại: biến thể tên + font
        var st = _viet.GetStatus(path);
        var font = _fonts.CurrentFontPak(path);
        StatusLine = st.Installed
            ? $"● Việt hóa: ĐÃ CÀI · Kiểu tên: {st.VariantLabel} · Font: {font ?? "mặc định"}"
            : "○ Việt hóa: CHƯA CÀI";
    }

    [RelayCommand]
    private async Task AddModAsync()
    {
        var path = _settings.Settings.GamePath;
        if (string.IsNullOrWhiteSpace(path)) { Message = "Chưa chọn thư mục game."; return; }
        var dlg = new OpenFileDialog { Title = "Chọn mod", Filter = "Gói VHWuWa (*.vhwpack)|*.vhwpack" };
        if (dlg.ShowDialog() != true) return;

        var conflicts = _mods.DetectConflicts(dlg.FileName);
        if (conflicts.Count > 0)
            Message = "Cảnh báo xung đột: " + string.Join("; ", conflicts.Take(3));

        Busy = true;
        try
        {
            var r = await _installer.InstallAsync(path, dlg.FileName);
            Message = r.Success ? "Đã cài mod." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; Refresh(); }
    }

    [RelayCommand]
    private async Task UninstallAsync()
    {
        if (SelectedMod is null) { Message = "Chọn một mod."; return; }
        Busy = true;
        try
        {
            var r = await _installer.UninstallAsync(_settings.Settings.GamePath, SelectedMod.PackageId);
            Message = r.Success ? "Đã gỡ mod." : "Lỗi: " + r.Error;
        }
        finally { Busy = false; Refresh(); }
    }

    [RelayCommand]
    private void OpenFolder()
    {
        var p = _settings.Settings.GamePath;
        if (Directory.Exists(p)) Process.Start(new ProcessStartInfo(p) { UseShellExecute = true });
    }
}
