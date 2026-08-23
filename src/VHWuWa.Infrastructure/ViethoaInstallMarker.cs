using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VHWuWa.Infrastructure;

/// <summary>
/// Ghi lại chính xác các file do bộ cài Việt hóa quản lý để không nhận nhầm hoặc xóa mod khác.
/// </summary>
internal sealed class ViethoaInstallMarker
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; } = 2;
    [JsonPropertyName("variant")] public string Variant { get; set; } = "";
    [JsonPropertyName("font")] public string? Font { get; set; }
    [JsonPropertyName("installedAt")] public DateTimeOffset InstalledAt { get; set; }
    [JsonPropertyName("managedFiles")] public Dictionary<string, string> ManagedFiles { get; set; }
        = new(StringComparer.OrdinalIgnoreCase);

    public static string ModKey(string fileName) => "mods/" + Path.GetFileName(fileName);
    public static string LoaderKey(string fileName) => "win64/" + Path.GetFileName(fileName);

    public static ViethoaInstallMarker? Load(string markerPath)
    {
        try
        {
            if (!File.Exists(markerPath)) return null;
            var marker = JsonSerializer.Deserialize<ViethoaInstallMarker>(File.ReadAllText(markerPath));
            if (marker is null) return null;
            marker.ManagedFiles = new Dictionary<string, string>(
                marker.ManagedFiles ?? new Dictionary<string, string>(), StringComparer.OrdinalIgnoreCase);
            return marker;
        }
        catch { return null; }
    }

    public void Save(string markerPath)
    {
        SchemaVersion = 2;
        InstalledAt = DateTimeOffset.Now;
        Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
        File.WriteAllText(markerPath, JsonSerializer.Serialize(this,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Track(string scope, string filePath)
    {
        if (!File.Exists(filePath)) return;
        var key = scope.Equals("win64", StringComparison.OrdinalIgnoreCase)
            ? LoaderKey(Path.GetFileName(filePath))
            : ModKey(Path.GetFileName(filePath));
        ManagedFiles[key] = Sha256(filePath);
    }

    public void ForgetModFile(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return;
        ManagedFiles.Remove(ModKey(fileName));
    }

    public bool Matches(string scope, string filePath)
    {
        if (!File.Exists(filePath)) return true;
        var key = scope.Equals("win64", StringComparison.OrdinalIgnoreCase)
            ? LoaderKey(Path.GetFileName(filePath))
            : ModKey(Path.GetFileName(filePath));
        return !ManagedFiles.TryGetValue(key, out var expected)
            || string.Equals(expected, Sha256(filePath), StringComparison.OrdinalIgnoreCase);
    }

    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
