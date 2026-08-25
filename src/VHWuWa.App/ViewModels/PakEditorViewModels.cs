using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VHWuWa.App.Services;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;

namespace VHWuWa.App.ViewModels;

public sealed class MatrixDataDto
{
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("items")] public List<MatrixItemDto> Items { get; set; } = [];
}

public sealed class MatrixItemDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("cn")] public string Cn { get; set; } = "";
    [JsonPropertyName("en")] public string En { get; set; } = "";
    [JsonPropertyName("default_name")] public string DefaultName { get; set; } = "";
    [JsonPropertyName("vi")] public string Vi { get; set; } = "";
    [JsonPropertyName("hv")] public string Hv { get; set; } = "";
    [JsonPropertyName("default_vi")] public string DefaultVi { get; set; } = "";
    [JsonPropertyName("default_hv")] public string DefaultHv { get; set; } = "";
    [JsonPropertyName("avatar")] public string Avatar { get; set; } = "";
    [JsonPropertyName("element")] public string Element { get; set; } = "";
    [JsonPropertyName("rarity")] public JsonElement Rarity { get; set; }
}

public partial class EditableMatrixRow : ObservableObject
{
    public string Id { get; init; } = "";
    public string Cn { get; init; } = "";
    public string En { get; init; } = "";
    public string DefaultName { get; init; } = "";
    public string DefaultVi { get; init; } = "";
    public string DefaultHv { get; init; } = "";
    public string AvatarPath { get; init; } = "";
    public string Element { get; init; } = "";
    public string Initial => string.IsNullOrWhiteSpace(En) ? "?" : En[..1].ToUpperInvariant();

    [ObservableProperty] private string _vi = "";
    [ObservableProperty] private string _hv = "";
    private string _originalVi = "";
    private string _originalHv = "";

    public bool ViDirty => !string.Equals(Vi.Trim(), _originalVi.Trim(), StringComparison.Ordinal);
    public bool HvDirty => !string.Equals(Hv.Trim(), _originalHv.Trim(), StringComparison.Ordinal);
    public bool IsDirty => ViDirty || HvDirty;

    partial void OnViChanged(string value) { OnPropertyChanged(nameof(ViDirty)); OnPropertyChanged(nameof(IsDirty)); }
    partial void OnHvChanged(string value) { OnPropertyChanged(nameof(HvDirty)); OnPropertyChanged(nameof(IsDirty)); }

    public void AcceptCurrent()
    {
        _originalVi = Vi;
        _originalHv = Hv;
        OnPropertyChanged(nameof(ViDirty));
        OnPropertyChanged(nameof(HvDirty));
        OnPropertyChanged(nameof(IsDirty));
    }

    public void RestoreDefaults()
    {
        Vi = DefaultVi;
        Hv = DefaultHv;
    }
}

public sealed record TermCategoryOption(string Id, string Title, int Count)
{
    public string Display => $"{Title} ({Count:N0} mục)";
}

public abstract partial class PakEditorViewModelBase : ObservableObject
{
    protected readonly PakEditorBridge Bridge;
    private readonly IViethoaInstaller _installer;
    private readonly ISettingsService _settings;

    [ObservableProperty] private bool _busy;
    [ObservableProperty] private string _status = "Đang chuẩn bị dữ liệu…";
    [ObservableProperty] private bool _canInstall;

    protected PakEditorViewModelBase(PakEditorBridge bridge, IViethoaInstaller installer,
        ISettingsService settings)
    {
        Bridge = bridge;
        _installer = installer;
        _settings = settings;
    }

    protected static MatrixDataDto ReadMatrix(System.Text.Json.Nodes.JsonObject response)
        => response["data"]?.Deserialize<MatrixDataDto>()
           ?? throw new InvalidDataException("Backend không trả danh sách chỉnh sửa.");

    protected async Task RunAsync(string workingText, Func<Task> action)
    {
        if (Busy) return;
        Busy = true;
        Status = workingText;
        try { await action(); }
        catch (Exception ex) { Status = "❌ " + ex.Message; }
        finally { Busy = false; }
    }

    [RelayCommand]
    private async Task RestorePaksAsync() => await RunAsync(
        "Đang tải và xác minh hai PAK mặc định từ GitHub…", async () =>
        {
            var result = await Bridge.RequestAsync(new { command = "restore-defaults" });
            CanInstall = true;
            Status = "✅ Đã khôi phục hai PAK mặc định từ " + (result["tag"]?.GetValue<string>() ?? "GitHub") + ".";
        });

    [RelayCommand]
    private async Task InstallViAsync() => await InstallAsync(NameVariant.English, "Việt hóa");

    [RelayCommand]
    private async Task InstallHvAsync() => await InstallAsync(NameVariant.HanViet, "Hán Việt");

    private async Task InstallAsync(NameVariant variant, string label)
        => await RunAsync($"Đang cài lại bản {label}…", async () =>
        {
            var game = _settings.Settings.GamePath;
            if (string.IsNullOrWhiteSpace(game))
                throw new InvalidOperationException("Chưa chọn thư mục game ở Trang chủ.");
            var result = await _installer.InstallAsync(game, variant, withFont: true);
            if (!result.Success) throw new InvalidOperationException(result.Error);
            Status = $"✅ Đã cài lại bản {label} vào game.";
        });

    protected static object ToPayload(EditableMatrixRow row) => new
    {
        cn = row.Cn, en = row.En, vi = row.Vi, hv = row.Hv,
        dirty_fields = new[] { row.ViDirty ? "vi" : "", row.HvDirty ? "hv" : "" }
            .Where(value => value.Length > 0).ToArray()
    };
}

public partial class CharacterNamesViewModel : PakEditorViewModelBase
{
    public ObservableCollection<EditableMatrixRow> Items { get; } = [];
    [ObservableProperty] private bool _loaded;
    public int DirtyCount => Items.Count(item => item.IsDirty);

    public CharacterNamesViewModel(PakEditorBridge bridge, IViethoaInstaller installer,
        ISettingsService settings) : base(bridge, installer, settings) { }

    public async Task LoadAsync(bool force = false)
    {
        if (Loaded && !force) return;
        await RunAsync("Đang nạp tên và icon nhân vật…", PopulateAsync);
    }

    private async Task PopulateAsync()
    {
        var matrix = ReadMatrix(await Bridge.RequestAsync(new { command = "matrix", category = "char" }));
        Items.Clear();
        foreach (var dto in matrix.Items)
        {
            var stored = (dto.Avatar ?? "").Replace('\\', '/');
            if (stored.StartsWith("avatars/", StringComparison.OrdinalIgnoreCase)) stored = stored[8..];
            if (string.IsNullOrWhiteSpace(stored))
                stored = new string(dto.En.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray()) + ".png";
            var row = new EditableMatrixRow
            {
                Id = dto.Id, Cn = dto.Cn, En = dto.En,
                DefaultName = string.IsNullOrWhiteSpace(dto.DefaultName) ? dto.En : dto.DefaultName,
                DefaultVi = dto.DefaultVi, DefaultHv = dto.DefaultHv,
                Vi = dto.Vi, Hv = dto.Hv, Element = dto.Element,
                AvatarPath = Path.Combine(Bridge.AvatarDirectory, stored)
            };
            row.AcceptCurrent();
            row.PropertyChanged += RowChanged;
            Items.Add(row);
        }
        Loaded = true;
        Status = $"Đã nạp {Items.Count:N0} nhân vật · chưa có thay đổi.";
        OnPropertyChanged(nameof(DirtyCount));
    }

    private void RowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditableMatrixRow.IsDirty))
            OnPropertyChanged(nameof(DirtyCount));
    }

    [RelayCommand]
    private async Task SaveBuildAsync()
    {
        var changed = Items.Where(item => item.IsDirty).ToList();
        if (changed.Count == 0) { Status = "Chưa có tên nào thay đổi."; return; }
        await RunAsync($"Đang lưu {changed.Count:N0} nhân vật và tạo lại hai PAK…", async () =>
        {
            await Bridge.RequestAsync(new { command = "save-build", category = "char", items = changed.Select(ToPayload).ToArray() });
            foreach (var item in changed) item.AcceptCurrent();
            OnPropertyChanged(nameof(DirtyCount));
            CanInstall = true;
            Status = "✅ Đã lưu tên và tạo lại đủ hai PAK. Chọn bản muốn cài vào game.";
        });
    }

    [RelayCommand]
    private async Task ResetBuildAsync()
    {
        if (MessageBox.Show("Trả toàn bộ tên VI/Hán Việt về mặc định và tạo lại hai PAK?",
                "VHWuWa", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        await RunAsync("Đang khôi phục tên mặc định và tạo lại hai PAK…", async () =>
        {
            await Bridge.RequestAsync(new { command = "reset-build", category = "char" });
            CanInstall = true;
            Loaded = false;
            await PopulateAsync();
            Status = "✅ Đã khôi phục tên mặc định và tạo lại hai PAK.";
        });
    }
}

public partial class TermsEditorViewModel : PakEditorViewModelBase
{
    private static readonly string[] Order =
        ["lore","location","combat","weapons","sonata","boss","endgame","ui_item","organization","npc"];
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["lore"]="Lore", ["location"]="Địa danh", ["combat"]="Chỉ số",
        ["weapons"]="Vũ khí", ["sonata"]="Sonata", ["boss"]="Boss",
        ["endgame"]="Endgame", ["ui_item"]="UI / Tiền tệ",
        ["organization"]="Phe phái", ["npc"]="Tên NPC"
    };

    public ObservableCollection<TermCategoryOption> Categories { get; } = [];
    public ObservableCollection<EditableMatrixRow> Items { get; } = [];
    [ObservableProperty] private TermCategoryOption? _selectedCategory;
    [ObservableProperty] private bool _loaded;
    private bool _suppressCategoryChange;
    private int _categoryLoadVersion;
    public int DirtyCount => Items.Count(item => item.IsDirty);

    public TermsEditorViewModel(PakEditorBridge bridge, IViethoaInstaller installer,
        ISettingsService settings) : base(bridge, installer, settings) { }

    partial void OnSelectedCategoryChanged(TermCategoryOption? value)
    {
        if (!_suppressCategoryChange && value is not null)
            _ = QueueCategoryLoadAsync(value.Id);
    }

    private async Task QueueCategoryLoadAsync(string category)
    {
        var version = ++_categoryLoadVersion;
        while (Busy) await Task.Delay(40);
        if (version == _categoryLoadVersion)
            await LoadCategoryAsync(category);
    }

    public async Task LoadAsync(bool force = false)
    {
        if (Loaded && !force) return;
        await RunAsync("Đang nạp danh sách loại thuật ngữ…", async () =>
        {
            var response = await Bridge.RequestAsync(new { command = "categories" });
            var all = response["items"]?.AsArray() ?? [];
            var counts = all.OfType<System.Text.Json.Nodes.JsonObject>().ToDictionary(
                item => item["id"]?.GetValue<string>() ?? "",
                item => item["count"]?.GetValue<int>() ?? 0);
            Categories.Clear();
            foreach (var id in Order.Where(counts.ContainsKey))
                Categories.Add(new TermCategoryOption(id, Labels[id], counts[id]));
            Loaded = true;
            var selected = SelectedCategory is not null
                ? Categories.FirstOrDefault(item => item.Id == SelectedCategory.Id)
                : Categories.FirstOrDefault();
            _suppressCategoryChange = true;
            SelectedCategory = selected;
            _suppressCategoryChange = false;
            if (selected is not null) await PopulateCategoryAsync(selected.Id);
        });
    }

    private async Task LoadCategoryAsync(string category)
        => await RunAsync("Đang nạp bảng " + Labels.GetValueOrDefault(category, category) + "…",
            () => PopulateCategoryAsync(category));

    private async Task PopulateCategoryAsync(string category)
    {
        var matrix = ReadMatrix(await Bridge.RequestAsync(new { command = "matrix", category }));
        Items.Clear();
        foreach (var dto in matrix.Items)
        {
            var row = new EditableMatrixRow
            {
                Id=dto.Id, Cn=dto.Cn, En=dto.En,
                DefaultName=dto.DefaultName, DefaultVi=dto.DefaultVi, DefaultHv=dto.DefaultHv,
                Vi=dto.Vi, Hv=dto.Hv, Element=dto.Element
            };
            row.AcceptCurrent();
            row.PropertyChanged += RowChanged;
            Items.Add(row);
        }
        OnPropertyChanged(nameof(DirtyCount));
        Status = $"{SelectedCategory?.Title ?? matrix.Title} · {Items.Count:N0} mục · sửa trực tiếp cột VI/HV.";
    }

    private void RowChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditableMatrixRow.IsDirty)) OnPropertyChanged(nameof(DirtyCount));
    }

    [RelayCommand]
    private void CopyEnglish(EditableMatrixRow? row)
    {
        var english = row?.En?.Trim();
        if (string.IsNullOrWhiteSpace(english))
        {
            Status = "Dòng này không có nội dung tiếng Anh để sao chép.";
            return;
        }
        try
        {
            Clipboard.SetDataObject(english, true);
            var preview = english.Length > 70 ? english[..67] + "…" : english;
            Status = $"📋 Đã sao chép EN: {preview}";
        }
        catch (Exception ex)
        {
            Status = "❌ Không truy cập được clipboard: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveBuildAsync()
    {
        var category = SelectedCategory?.Id;
        var changed = Items.Where(item => item.IsDirty).ToList();
        if (category is null || changed.Count == 0) { Status = "Chưa có thuật ngữ nào thay đổi."; return; }
        await RunAsync($"Đang đối chiếu đúng key, lưu {changed.Count:N0} mục và tạo lại hai PAK…", async () =>
        {
            await Bridge.RequestAsync(new { command="save-build", category, items=changed.Select(ToPayload).ToArray() });
            foreach (var item in changed) item.AcceptCurrent();
            OnPropertyChanged(nameof(DirtyCount));
            CanInstall = true;
            Status = "✅ Đã lưu đúng key và tạo lại đủ hai PAK. Chọn bản muốn cài.";
        });
    }

    [RelayCommand]
    private async Task ResetBuildAsync()
    {
        var category = SelectedCategory?.Id;
        if (category is null) return;
        if (MessageBox.Show($"Khôi phục toàn bộ loại “{SelectedCategory!.Title}” và tạo lại hai PAK?",
                "VHWuWa", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        await RunAsync("Đang khôi phục phân khu và tạo lại hai PAK…", async () =>
        {
            await Bridge.RequestAsync(new { command="reset-build", category });
            CanInstall = true;
            await PopulateCategoryAsync(category);
            Status = "✅ Đã khôi phục phân khu và tạo lại hai PAK.";
        });
    }
}
