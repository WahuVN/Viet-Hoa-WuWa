using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VHWuWa.Core.Models;

namespace VHWuWa.Infrastructure;

/// <summary>
/// R29 font PAK mount. Owns only wahufont/{WahuFont_100_P.pak,.sig,receipt.json}
/// and Mount/wahufont.txt. Never enumerates or modifies stock MountOption_Pack_*,
/// MountLauncher, other font mods, or the translation's wahuviethoa sidecar.
/// </summary>
public static class R29FontMountedPak
{
    private const string Folder = "wahufont";
    private const string PakFile = "WahuFont_100_P.pak";
    private const string SigFile = "WahuFont_100_P.sig";
    private const string MountFile = "wahufont.txt";
    private const string ReceiptFile = "font-receipt.json";

    private sealed record Receipt(int Schema, string SourceName, string PakSha256, string SigSha256);

    private static string ResourceDir(string game) =>
        Path.Combine(game, "Client", "Saved", "Resources", "3.8.0");

    private static string FontDir(string game) => Path.Combine(ResourceDir(game), Folder);
    private static string MountDir(string game) => Path.Combine(ResourceDir(game), "Mount");
    private static string Pak(string game) => Path.Combine(FontDir(game), PakFile);
    private static string Sig(string game) => Path.Combine(FontDir(game), SigFile);
    private static string Sidecar(string game) => Path.Combine(MountDir(game), MountFile);
    private static string Manifest(string game) => Path.Combine(FontDir(game), ReceiptFile);

    private static string Sha(string file, bool sha1 = false)
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(sha1 ? SHA1.HashData(stream) : SHA256.HashData(stream));
    }

    private static string MountText(string pakSha1, string sigSha1) =>
        "::Mount::\r\nwahufont/WahuFont_100_P,100," +
        pakSha1 + "," + sigSha1 + ",,\r\n::Del::\r\n";

    private static bool IsLink(string path)
    {
        try
        {
            return (File.Exists(path) || Directory.Exists(path))
                && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch { return true; }
    }

    private static bool PathsSafe(string game)
    {
        foreach (var path in new[] { game, Path.Combine(game, "Client"),
                     Path.Combine(game, "Client", "Saved"),
                     ResourceDir(game), MountDir(game), FontDir(game),
                     Pak(game), Sig(game), Sidecar(game), Manifest(game) })
            if (IsLink(path)) return false;
        return true;
    }

    private static Receipt? VerifiedReceipt(string game)
    {
        try
        {
            if (!PathsSafe(game) || !File.Exists(Manifest(game))
                || !File.Exists(Pak(game)) || !File.Exists(Sig(game))
                || !File.Exists(Sidecar(game))) return null;

            var receipt = JsonSerializer.Deserialize<Receipt>(File.ReadAllText(Manifest(game)));
            if (receipt is null || receipt.Schema != 1
                || string.IsNullOrWhiteSpace(receipt.SourceName)
                || receipt.SourceName != Path.GetFileName(receipt.SourceName)
                || !receipt.SourceName.EndsWith("_100_P.pak", StringComparison.OrdinalIgnoreCase))
                return null;
            if (!Sha(Pak(game)).Equals(receipt.PakSha256, StringComparison.OrdinalIgnoreCase)
                || !Sha(Sig(game)).Equals(receipt.SigSha256, StringComparison.OrdinalIgnoreCase))
                return null;

            var expected = MountText(Sha(Pak(game), sha1: true), Sha(Sig(game), sha1: true));
            return File.ReadAllText(Sidecar(game), Encoding.UTF8) == expected
                ? receipt : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                      or InvalidDataException or JsonException or ArgumentException)
        {
            return null;
        }
    }

    public static bool IsInstalled(string game) => VerifiedReceipt(game) is not null;
    public static string? CurrentFontPak(string game) => VerifiedReceipt(game)?.SourceName;

    public static Result Install(string game, string fontPakPath) =>
        InstallCore(game, fontPakPath);

    private static Result InstallCore(string game, string fontPakPath)
    {
        var staged = new List<string>();
        var movedOld = new List<(string Original, string Backup)>();
        var installedNew = new List<string>();
        try
        {
            if (string.IsNullOrWhiteSpace(fontPakPath) || !File.Exists(fontPakPath))
                return Result.Fail("Không tìm thấy gói font PAK.");
            var sourceName = Path.GetFileName(fontPakPath);
            if (!sourceName.EndsWith("_100_P.pak", StringComparison.OrdinalIgnoreCase)
                || sourceName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return Result.Fail("Chọn đúng gói font *_100_P.pak.");
            if (new FileInfo(fontPakPath).Length > 64L * 1024 * 1024)
                return Result.Fail("PAK font vượt giới hạn an toàn 64 MiB.");
            if (!PakV12Converter.TryVerifyV12(fontPakPath, out var verifyError))
                return Result.Fail("PAK font không hợp lệ: " + verifyError);
            if (!PathsSafe(game)) return Result.Fail("Đường dẫn game chứa liên kết ngoài; không thể cài an toàn.");

            var sigSource = R29MountedPak.StockSig(game);
            if (sigSource is null) return Result.Fail("Không xác minh được SIG nguồn đang được game sử dụng.");
            var paths = new[] { Pak(game), Sig(game), Manifest(game), Sidecar(game) };
            var sourceSha = Sha(fontPakPath);
            var current = VerifiedReceipt(game);
            if (current is not null
                && sourceSha.Equals(current.PakSha256, StringComparison.OrdinalIgnoreCase))
                return Result.Ok();
            // Existing unverified font files may belong to another mod or a changed
            // installation. Never overwrite them without a valid WAHU receipt.
            if (current is null && paths.Any(File.Exists))
                return Result.Fail("Có tệp font tại vị trí WAHU nhưng không xác minh được chủ sở hữu. "
                    + "Không ghi đè; hãy kiểm tra bản sao lưu và mod đang dùng.");

            Directory.CreateDirectory(FontDir(game));
            Directory.CreateDirectory(MountDir(game));

            var suffix = ".wahu-" + Guid.NewGuid().ToString("N");
            var stagePaths = paths.Select(p => p + suffix + ".new").ToArray();
            staged.AddRange(stagePaths);
            File.Copy(fontPakPath, stagePaths[0], overwrite: false);
            if (!Sha(stagePaths[0]).Equals(sourceSha, StringComparison.OrdinalIgnoreCase))
                throw new IOException("PAK font copy không trùng SHA-256.");
            File.Copy(sigSource, stagePaths[1], overwrite: false);
            var sigSha = Sha(stagePaths[1]);
            var receipt = new Receipt(1, sourceName, sourceSha, sigSha);
            File.WriteAllText(stagePaths[2], JsonSerializer.Serialize(receipt), new UTF8Encoding(false));
            File.WriteAllText(stagePaths[3],
                MountText(Sha(stagePaths[0], true), Sha(stagePaths[1], true)),
                new UTF8Encoding(false));

            for (var i = 0; i < paths.Length; i++)
            {
                if (File.Exists(paths[i]))
                {
                    var backup = paths[i] + suffix + ".old";
                    File.Move(paths[i], backup);
                    movedOld.Add((paths[i], backup));
                }
            }
            for (var i = 0; i < paths.Length; i++)
            {
                File.Move(stagePaths[i], paths[i]);
                installedNew.Add(paths[i]);
            }

            if (VerifiedReceipt(game)?.SourceName != sourceName)
                throw new IOException("Font Mount hậu kiểm SHA-256 thất bại.");
            foreach (var (_, backup) in movedOld)
                if (File.Exists(backup)) File.Delete(backup);
            movedOld.Clear();
            installedNew.Clear();
            return Result.Ok();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException
                                      or ArgumentException or InvalidDataException)
        {
            foreach (var path in installedNew)
                try { if (File.Exists(path)) File.Delete(path); } catch { }
            foreach (var (original, backup) in movedOld.AsEnumerable().Reverse())
                try { if (File.Exists(backup) && !File.Exists(original)) File.Move(backup, original); } catch { }
            return Result.Fail("Cài font R29 thất bại: " + e.Message, e);
        }
        finally
        {
            foreach (var file in staged)
                try { if (File.Exists(file)) File.Delete(file); } catch { }
        }
    }

    public static Result Remove(string game)
    {
        try
        {
            if (!File.Exists(Pak(game)) && !File.Exists(Sig(game))
                && !File.Exists(Sidecar(game)) && !File.Exists(Manifest(game)))
                return Result.Ok(); // Font game mặc định, không cần gỡ.
            if (VerifiedReceipt(game) is null)
                return Result.Fail("Không nhận diện được font R29 đã cài; không xóa file lạ.");
            // Disable the sidecar first; touch only four verified, owned files.
            File.Delete(Sidecar(game));
            File.Delete(Pak(game));
            File.Delete(Sig(game));
            File.Delete(Manifest(game));
            if (!Directory.EnumerateFileSystemEntries(FontDir(game)).Any())
                Directory.Delete(FontDir(game));
            return Result.Ok();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return Result.Fail("Không thể gỡ font R29: " + e.Message, e);
        }
    }
}
