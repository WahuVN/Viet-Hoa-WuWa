using System.Security.Cryptography;

namespace VHWuWa.Core.Services;

/// <summary>
/// Migrates only byte-for-byte known, owned assets from prior public releases
/// after a verified R29 release becomes active. Never touches game directories
/// or unknown/user-modified files. Originals are backed up outside the app
/// directory before being deactivated.
/// </summary>
public static class LegacyReleaseAssetMigration
{
    public sealed record MigrationResult(int BackedUp, int UnknownPreserved, IReadOnlyList<string> Errors);

    private static readonly IReadOnlyDictionary<string, string> OwnedLegacyAssets =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["content/WuWaVH_EN_99_P.pak"] = "44350c79cbf9cb307752682f0e0f3a31af983cdc1fea4b24403b7395237df547",
            ["content/WuWaVH_HanViet_99_P.pak"] = "e41177b5860465dfb40fbe2f3fdb4504be1a814a3bfd9bfa4a9da736d0a5e9e2",
            ["content/font/WahuFont_100_P.pak"] = "ad51a1a0d3cc8abc1be43c13576d1b0f1adf2f3bf3b271c10aec836864dd14dc",
            ["content/loader/version.dll"] = "b328db7a57f3fba676b807112432d462ef7fe8c1b49e1572f89a1fbacaec48b3",
            ["content/loader/verorg.dll"] = "cea99f212af557a9613150c5509631403ed0f05f5d9a06ac42039bd59b40e39a",
            ["content/loader/WuWaVH.dll"] = "ba4bcbea10ef98c268683df3f87bd1e5090cf9245eb886d84c84ac82e25e4e5b",

            // Public v3.0.24 files copied forward by the old overlay updater.
            ["content/3.8.11/WuWaVH_PC_R29_99_P.pak"] = "38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598",
            ["content/font/Lexend-660_100_P.pak"] = "6be60ffc39ce0ee0a31e7dfbda0acc9e243f395ef94a65609baa8146ba62ca68",
            ["editor/WahuPakEditor.exe"] = "0c03d67908ae0b80dbb7c10af181af578e6452bd4dd32406a1bfc7aa7319f195"
        };

    public static MigrationResult BackupAndDeactivate(
        string appDirectory, string backupDirectory, bool verifiedR29)
    {
        if (!verifiedR29)
            return new MigrationResult(0, 0, Array.Empty<string>());

        var root = Path.GetFullPath(appDirectory);
        var backupRoot = Path.GetFullPath(backupDirectory);
        if (string.Equals(root, backupRoot, StringComparison.OrdinalIgnoreCase)
            || PathValidation.IsSubPathOf(backupRoot, root))
            throw new InvalidDataException("Thư mục backup phải nằm ngoài thư mục ứng dụng.");

        var errors = new List<string>();
        var preserved = 0;
        var backedUp = 0;
        foreach (var entry in OwnedLegacyAssets)
        {
            var relative = entry.Key.Replace('/', Path.DirectorySeparatorChar);
            var source = Path.Combine(root, relative);
            if (!File.Exists(source)) continue;

            try
            {
                // Refuse links and paths through linked folders. No writes via junctions.
                var folder = Path.GetDirectoryName(source);
                while (folder is not null && PathValidation.IsSubPathOf(folder, root))
                {
                    if (File.Exists(folder) || (Directory.Exists(folder)
                        && (File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0))
                        throw new IOException("Không xử lý liên kết trong thư mục app.");
                    if (string.Equals(folder, root, StringComparison.OrdinalIgnoreCase)) break;
                    folder = Path.GetDirectoryName(folder);
                }
                if ((File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("Không xử lý file liên kết.");

                if (!Matches(source, entry.Value))
                {
                    preserved++;
                    continue;
                }

                var destination = Path.Combine(backupRoot, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                if (File.Exists(destination))
                {
                    if (!Matches(destination, entry.Value))
                        throw new IOException("Backup có file khác dữ liệu bản phát hành.");
                }
                else
                {
                    var temp = destination + ".tmp-" + Guid.NewGuid().ToString("N");
                    try
                    {
                        File.Copy(source, temp, overwrite: false);
                        if (!Matches(temp, entry.Value))
                            throw new IOException("Backup không khớp SHA-256, giữ nguyên file.");
                        File.Move(temp, destination);
                    }
                    finally
                    {
                        try { if (File.Exists(temp)) File.Delete(temp); } catch { }
                    }
                }

                // Only after a verified duplicate exists outside the app tree.
                if (!Matches(source, entry.Value))
                    throw new IOException("File nguồn thay đổi trong khi sao lưu.");
                File.Delete(source);
                backedUp++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                errors.Add(entry.Key + ": " + ex.Message);
            }
        }
        return new MigrationResult(backedUp, preserved, errors);
    }

    private static bool Matches(string file, string expectedSha256)
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexString(SHA256.HashData(stream))
            .Equals(expectedSha256, StringComparison.OrdinalIgnoreCase);
    }
}
