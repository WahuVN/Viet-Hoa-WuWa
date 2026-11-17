using System.Security.Cryptography;
using System.Text.Json;

namespace VHWuWa.Core.Services;

/// <summary>
/// Read-only evidence for the exact PC R28 offline PAK. A valid checksum does NOT authorize
/// applying it to the game: loader and executable compatibility need separate certification.
/// </summary>
public static class StagedPakInspector
{
    public const string ResourceVersion = "3.8.10";
    public const string FileName = "WuWaVH_38_ENVI_R28_99_P.pak";
    public const string ExpectedSha256 = "34dc39482394723fe1ad65dc48a02a5c5b88483a89cee926463963b57d15d64f";
    public const long ExpectedFileBytes = 125532883;
    public const int ExpectedEntries = 62;

    public sealed record Result(bool Present, bool ShaVerified, string Message)
    {
        public bool CanInstallToGame => false;
    }

    public static Result Inspect(string applicationDirectory)
    {
        var content = Path.Combine(applicationDirectory, "content", ResourceVersion);
        var manifest = Path.Combine(content, "R28_PREUPDATE_MANIFEST.json");
        if (!File.Exists(manifest))
            return new Result(false, false, "Chưa có gói PAK PC 3.8.10.");

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
            var j = doc.RootElement;
            if (j.ValueKind != JsonValueKind.Object
                || Value(j, "schema") != "WAHU_PC_R28_PREUPDATE_NOT_INSTALLABLE_UNTIL_LOADER_VERIFIED"
                || Value(j, "game_version") != "3.8.1"
                || Value(j, "resource_version") != ResourceVersion
                || Value(j, "content_release") != "R28"
                || Value(j, "pak_path") != $"content/{ResourceVersion}/{FileName}"
                || !string.Equals(Value(j, "pak_sha256"), ExpectedSha256, StringComparison.OrdinalIgnoreCase)
                || !j.TryGetProperty("pak_entries", out var entries)
                || !entries.TryGetInt32(out var total)
                || total != ExpectedEntries
                || !j.TryGetProperty("automatic_install_enabled", out var auto)
                || auto.ValueKind != JsonValueKind.False
                || !j.TryGetProperty("loader_3_8_verified", out var loader)
                || loader.ValueKind != JsonValueKind.False)
                return new Result(true, false, "PAK R28 có manifest không hợp lệ. Không thể sử dụng.");

            var pak = Path.Combine(content, FileName);
            if (!File.Exists(pak))
                return new Result(true, false, "Có thông tin PAK R28 nhưng thiếu tệp dữ liệu.");
            if (new FileInfo(pak).Length != ExpectedFileBytes)
                return new Result(true, false, "PAK R28 sai dung lượng, chưa thể sử dụng.");

            using var input = File.OpenRead(pak);
            var digest = Convert.ToHexString(SHA256.HashData(input));
            if (!digest.Equals(ExpectedSha256, StringComparison.OrdinalIgnoreCase))
                return new Result(true, false, "PAK R28 sai SHA-256, đã khóa cài đặt.");
            return new Result(true, true,
                "PAK R28 (3.8.10) có SHA hợp lệ, nhưng thử nghiệm PC 3.8.0/resource 3.8.11 báo Corrupt pak index. Không cài.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                                    or JsonException or ArgumentException)
        {
            return new Result(true, false, "Không kiểm tra được PAK R28: " + ex.Message);
        }
    }

    private static string? Value(JsonElement json, string property)
        => json.TryGetProperty(property, out var item) && item.ValueKind == JsonValueKind.String
            ? item.GetString()
            : null;
}
