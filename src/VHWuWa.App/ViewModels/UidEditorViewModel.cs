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
        await RunAsync("Đang nạp thiết lập UID và 4 trường dữ liệu game…", async () =>
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
                ? "UID hiện tại đang ẨN (khoảng trắng). Bạn có thể nhập tên tùy thích hoặc để trống."
                : $"Tên UID hiện tại: “{CustomUid}”. Đã nạp 4 trường hiển thị trong game.";
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
            ? "Đang lưu cấu hình ẩn UID và tạo lại hai PAK…"
            : $"Đang lưu tên UID “{text}” và tạo lại hai PAK…", async () =>
        {
            await Bridge.RequestAsync(new { command = "save-uid", uid_text = text });
            _originalUid = text;
            CustomUid = text;
            OnPropertyChanged(nameof(IsUidDirty));
            UpdateInstallPrompt(true, true, true);
            Status = string.IsNullOrWhiteSpace(text)
                ? "✅ Đã đặt lại ẩn UID mặc định và tạo xong 2 gói PAK trong app\\content\\."
                : $"✅ Đã đặt tên UID thành “{text}” và tạo xong 2 gói PAK trong app\\content\\.";
            
            Loaded = false;
            await LoadAsync(true);
        });
    }

    [RelayCommand]
    private async Task ResetUidAsync()
    {
        await RunAsync("Đang khôi phục ẩn UID mặc định và tạo lại hai PAK…", async () =>
        {
            await Bridge.RequestAsync(new { command = "reset-uid" });
            CustomUid = "";
            _originalUid = "";
            OnPropertyChanged(nameof(IsUidDirty));
            UpdateInstallPrompt(true, true, true);
            Status = "✅ Đã khôi phục ẩn UID mặc định và tạo lại hai PAK.";
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

        await RunAsync("Đang lưu cấu hình UID và tạo lại 2 bản PAK…", async () =>
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
            Status = "✅ Đã lưu cấu hình UID và tạo xong 2 gói PAK (Việt hóa & Hán Việt).";
        });
    }
}
