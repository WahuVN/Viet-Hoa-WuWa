using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VHWuWa.App.Services;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public partial class UidEditorViewModel : PakEditorViewModelBase
{
    public ObservableCollection<EditableMatrixRow> Items { get; } = [];
    [ObservableProperty] private bool _loaded;
    [ObservableProperty] private string _customUid = "";
    private string _originalUid = "";

    public bool IsUidDirty => !string.Equals((CustomUid ?? "").Trim(), (_originalUid ?? "").Trim(), StringComparison.Ordinal);
    public int DirtyCount => Items.Count(item => item.IsDirty);

    partial void OnCustomUidChanged(string value) => OnPropertyChanged(nameof(IsUidDirty));

    public UidEditorViewModel(PakEditorBridge bridge, IViethoaInstaller installer,
        ISettingsService settings) : base(bridge, installer, settings) { }

    public async Task LoadAsync(bool force = false)
    {
        if (Loaded && !force) return;
        await RunAsync("Đang đọc thiết lập UID…", async () =>
        {
            try
            {
                var uidResponse = await Bridge.RequestAsync(new { command = "get-uid" });
                var uidVal = uidResponse["uid_text"]?.GetValue<string>() ?? "";
                _originalUid = uidVal;
                CustomUid = uidVal;
                OnPropertyChanged(nameof(IsUidDirty));
            }
            catch { }

            var matrix = ReadMatrix(await Bridge.RequestAsync(new { command = "matrix", category = "uid" }));
            Items.Clear();
            foreach (var dto in matrix.Items)
            {
                var row = new EditableMatrixRow
                {
                    Id = dto.Id, Cn = dto.Cn, En = dto.En,
                    DefaultName = dto.DefaultName, DefaultVi = dto.DefaultVi, DefaultHv = dto.DefaultHv,
                    Vi = dto.Vi, Hv = dto.Hv, Element = dto.Element
                };
                row.AcceptCurrent();
                row.PropertyChanged += RowChanged;
                Items.Add(row);
            }
            OnPropertyChanged(nameof(DirtyCount));
            Loaded = true;
            Status = string.IsNullOrWhiteSpace(CustomUid)
                ? "UID đang ẩn."
                : $"Tên đang dùng: {CustomUid}";
        });
    }

    private void RowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditableMatrixRow.IsDirty)) OnPropertyChanged(nameof(DirtyCount));
    }

    [RelayCommand]
    private async Task SaveUidAsync()
    {
        var text = (CustomUid ?? "").Trim();
        await RunAsync(string.IsNullOrWhiteSpace(text)
            ? "Đang ẩn UID và tạo PAK…"
            : "Đang lưu UID và tạo PAK…", async () =>
        {
            await Bridge.RequestAsync(new { command = "save-uid", uid_text = text });
            _originalUid = text;
            CustomUid = text;
            OnPropertyChanged(nameof(IsUidDirty));
            UpdateInstallPrompt(true, true, true);
            Status = string.IsNullOrWhiteSpace(text)
                ? "Đã ẩn UID. PAK đã sẵn sàng."
                : "Đã lưu tên UID. PAK đã sẵn sàng.";
            
            Loaded = false;
            await LoadAsync(true);
        });
    }

    [RelayCommand]
    private async Task ResetUidAsync()
    {
        await RunAsync("Đang ẩn UID và tạo PAK…", async () =>
        {
            await Bridge.RequestAsync(new { command = "reset-uid" });
            CustomUid = "";
            _originalUid = "";
            OnPropertyChanged(nameof(IsUidDirty));
            UpdateInstallPrompt(true, true, true);
            Status = "Đã ẩn UID. PAK đã sẵn sàng.";
            Loaded = false;
            await LoadAsync(true);
        });
    }

    [RelayCommand]
    private async Task SaveBuildAsync()
    {
        var changed = Items.Where(item => item.IsDirty).ToList();
        var uidChanged = IsUidDirty;
        if (changed.Count == 0 && !uidChanged)
        {
            Status = "Chưa có thay đổi nào cần lưu.";
            return;
        }

        await RunAsync("Đang lưu bảng UID và tạo PAK…", async () =>
        {
            var req = new Dictionary<string, object>
            {
                ["command"] = "save-build",
                ["category"] = "uid",
                ["items"] = changed.Select(ToPayload).ToArray(),
                ["modes"] = new[] { "vi", "hv" }
            };
            if (uidChanged)
            {
                req["uid_text"] = (CustomUid ?? "").Trim();
            }
            await Bridge.RequestAsync(req);
            foreach (var item in changed) item.AcceptCurrent();
            if (uidChanged)
            {
                _originalUid = (CustomUid ?? "").Trim();
                OnPropertyChanged(nameof(IsUidDirty));
            }
            OnPropertyChanged(nameof(DirtyCount));
            UpdateInstallPrompt(true, true, true);
            Status = "Đã lưu bảng UID. PAK đã sẵn sàng.";
        });
    }
}
