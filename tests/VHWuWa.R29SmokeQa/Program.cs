using System.Security.Cryptography;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Infrastructure;

if (args.Length != 1)
    throw new ArgumentException("Usage: VHWuWa.R29SmokeQa <real-R29-pak>");
var realPak = Path.GetFullPath(args[0]);
const string expected = "38527e34ffeb8ef7538206d3d4dbd5cbac54a1e6cd9d0957501035ff41a8e598";
using (var source = File.OpenRead(realPak))
{
    var sha = Convert.ToHexString(SHA256.HashData(source)).ToLowerInvariant();
    if (sha != expected) throw new InvalidDataException("R29 source PAK SHA mismatch");
}
var work = Path.Combine(Path.GetTempPath(), "WahuR29SmokeQa_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(work);

void Scenario(string mode, bool officialMount, bool englishSig, bool staleWahu,
    bool japaneseSig = false, bool tamperJapaneseMount = false, bool omitJapaneseMount = false)
{
    var root = Path.Combine(work, mode);
    var game = japaneseSig
        ? Path.Combine(root, "SteamLibrary", "steamapps", "common", "Wuthering Waves", "Wuthering Waves Game")
        : Path.Combine(root, "Wuthering Waves Game");
    var app = Path.Combine(root, "portable-v321");
    var exe = Path.Combine(game, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
    var res = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0");
    var sig = Path.Combine(res, "Lang_en", mode == "base" ? "Base" : "3.8.11",
        "pakchunk10-WindowsNoEditor_P.sig");
    Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
    File.WriteAllBytes(exe, new byte[] { 1, 3, 3, 7 });
    if (englishSig)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(sig)!);
        File.WriteAllBytes(sig, new byte[] { 10, 20, 30, 40, 50 });
    }
    var japaneseStockSig = Path.Combine(res, "Lang_ja", "3.8.11",
        "pakchunk10-WindowsNoEditor_P.sig");
    if (japaneseSig)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(japaneseStockSig)!);
        File.WriteAllBytes(japaneseStockSig, new byte[] { 20, 16, 17, 11, 29, 99 });
    }
    var originalJapaneseSig = japaneseSig ? File.ReadAllBytes(japaneseStockSig) : null;
    var originalExeSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(exe)));
    var originalSigSha = englishSig
        ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sig)))
        : null;
    var stockMount = Path.Combine(res, "Mount", "MountLang_en.txt");
    if (officialMount)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(stockMount)!);
        File.WriteAllText(stockMount, "::Mount::\r\n");
    }
    var originalMount = File.Exists(stockMount) ? File.ReadAllBytes(stockMount) : null;
    var japaneseMount = Path.Combine(res, "Mount", "MountLang_ja.txt");
    byte[]? originalJapaneseMount = null;
    if (japaneseSig && !omitJapaneseMount)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(japaneseMount)!);
        var jaHash = Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(japaneseStockSig)));
        var officialSha = tamperJapaneseMount ? new string('A', 40) : jaHash;
        File.WriteAllText(japaneseMount, "::Mount::\r\nLang_ja/3.8.11/pakchunk10-WindowsNoEditor_P,10,PAKSHA,"
            + officialSha + ",,\r\n::Del::\r\n");
        originalJapaneseMount = File.ReadAllBytes(japaneseMount);
    }

    var sidecar = Path.Combine(res, "Mount", "wahuviethoa.txt");
    var dest = Path.Combine(res, "wahuviethoa", "WuWaVH_99_P.pak");
    var destSig = Path.Combine(res, "wahuviethoa", "WuWaVH_99_P.sig");
    if (staleWahu)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        Directory.CreateDirectory(Path.GetDirectoryName(sidecar)!);
        File.WriteAllBytes(dest, new byte[] { 9, 9, 9 });
        File.WriteAllBytes(destSig, new byte[] { 8, 8 });
        File.WriteAllText(sidecar, "stale-r29");
        File.SetAttributes(dest, FileAttributes.ReadOnly | FileAttributes.Hidden);
        File.SetAttributes(destSig, FileAttributes.ReadOnly);
        File.SetAttributes(sidecar, FileAttributes.ReadOnly);
    }

    var oldPak = File.Exists(dest) ? File.ReadAllBytes(dest) : null;
    var oldSig = File.Exists(destSig) ? File.ReadAllBytes(destSig) : null;
    var oldSidecar = File.Exists(sidecar) ? File.ReadAllBytes(sidecar) : null;

    var package = Path.Combine(app, "content", "3.8.11", "WuWaVH_PC_R29_99_P.pak");
    Directory.CreateDirectory(Path.GetDirectoryName(package)!);
    File.Copy(realPak, package, overwrite: false);

    if (!R29MountedPak.PackageVerified(app))
        throw new Exception("Package SHA-256 preflight failed for " + mode);
    var expectedCompatible = englishSig || (japaneseSig && !tamperJapaneseMount && !omitJapaneseMount);
    if (R29MountedPak.GameCompatible(game) != expectedCompatible)
        throw new Exception("SIG provenance preflight mismatch for " + mode);
    var result = R29MountedPak.Install(app, game);
    if (!expectedCompatible)
    {
        if (result.Success) throw new Exception("Invalid SIG provenance was accepted: " + mode);
        bool Unchanged(string p, byte[]? before) =>
            before is null ? !File.Exists(p) : File.Exists(p) && File.ReadAllBytes(p).SequenceEqual(before);
        if (!Unchanged(dest, oldPak) || !Unchanged(destSig, oldSig) || !Unchanged(sidecar, oldSidecar))
            throw new Exception("SIG preflight modified files before refusing install: " + mode);
        if (japaneseSig && (originalJapaneseSig is null
            || !File.ReadAllBytes(japaneseStockSig).SequenceEqual(originalJapaneseSig)))
            throw new Exception("Rejected SIG was modified");
        Console.WriteLine("PASS R29_INVALID_SIG_FAIL_CLOSED=" + mode);
        return;
    }
    if (!result.Success) throw new Exception("Install failed for " + mode + ": " + result.Error);
    if (!R29MountedPak.IsInstalled(game)) throw new Exception("Installed files not verified");
    if (!File.Exists(sidecar) || !File.Exists(dest) || !File.Exists(destSig))
        throw new Exception("R29 sidecar/pak/sig absent");
    var destSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dest))).ToLowerInvariant();
    if (destSha != expected) throw new Exception("Staged R29 pak hash mismatch");
    if (japaneseSig)
    {
        if (originalJapaneseSig is null || !File.ReadAllBytes(destSig).SequenceEqual(originalJapaneseSig))
            throw new Exception("Steam without Lang_en did not inherit intact Lang_ja signature");
    }
    else if (englishSig && !File.ReadAllBytes(destSig).SequenceEqual(File.ReadAllBytes(sig)))
        throw new Exception("Kuro Lang_en signature was not copied intact");

    var undo = R29MountedPak.Uninstall(game);
    if (!undo.Success) throw new Exception("Uninstall failed for " + mode + ": " + undo.Error);
    if (File.Exists(sidecar) || File.Exists(dest) || File.Exists(destSig) || R29MountedPak.IsInstalled(game))
        throw new Exception("R29 files left after uninstall");
    if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(exe))) != originalExeSha)
        throw new Exception("Stock EXE changed");
    if (englishSig)
    {
        if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sig))) != originalSigSha)
            throw new Exception("Stock English SIG changed");
    }
    else if (File.Exists(sig))
    {
        throw new Exception("Installer unexpectedly created stock English SIG");
    }
    if (japaneseSig && (originalJapaneseSig is null
        || !File.ReadAllBytes(japaneseStockSig).SequenceEqual(originalJapaneseSig)))
        throw new Exception("Official Japanese SIG was modified");
    if (originalJapaneseMount is not null
        && !File.ReadAllBytes(japaneseMount).SequenceEqual(originalJapaneseMount))
        throw new Exception("Official Japanese Mount was modified");
    if ((originalMount is null) != (!File.Exists(stockMount))
        || (originalMount is not null && !File.ReadAllBytes(stockMount).SequenceEqual(originalMount)))
        throw new Exception("Official Mount changed");
    Console.WriteLine("PASS R29_INSTALL_UNINSTALL=" + mode
        + " ENGLISH_SIG=" + englishSig
        + " STOCK_JAPANESE_SIG=" + japaneseSig
        + " STALE_WAHU_OVERWRITE=" + staleWahu
        + " MOUNT_ORIGINAL_PRESERVED=true");
}

// Negative QA: corrupt R29 must leave all managed files and mod preimages untouched.
async Task InvalidPackagePreflightScenario()
{
    var source = Path.Combine(AppContext.BaseDirectory, "content", "3.8.11", "WuWaVH_PC_R29_99_P.pak");
    var previousExists = File.Exists(source);
    var previousBytes = previousExists ? File.ReadAllBytes(source) : null;
    var game = Path.Combine(work, "invalid-r29", "game");
    var exe = Path.Combine(game, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
    var mount = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0", "Mount", "wahuviethoa.txt");
    Directory.CreateDirectory(Path.GetDirectoryName(source)!);
    Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
    Directory.CreateDirectory(Path.GetDirectoryName(mount)!);
    File.WriteAllBytes(exe, new byte[] { 3, 2, 1 });
    File.WriteAllText(mount, "original-sidecar");
    try
    {
        File.WriteAllBytes(source, new byte[] { 9, 9, 9 });
        var installer = new ViethoaInstaller(new NullLog(),
            quarantineRoot: Path.Combine(work, "quarantine-invalid"), isGameRunning: () => false);
        if ((await installer.InstallAsync(game, NameVariant.English, withFont: false)).Success)
            throw new Exception("Corrupt R29 was accepted by high-level installer");
        if (R29MountedPak.Install(AppContext.BaseDirectory, game).Success)
            throw new Exception("Corrupt R29 was accepted by direct installer");
        if (!File.Exists(mount) || File.ReadAllText(mount) != "original-sidecar")
            throw new Exception("Invalid-package preflight mutated original sidecar");
        if (Directory.Exists(Path.Combine(work, "quarantine-invalid")))
            throw new Exception("Invalid-package preflight created unexpected quarantine");
        Console.WriteLine("PASS R29_CORRUPT_PACKAGE_PRESERVES_PREIMAGES=true");
    }
    finally
    {
        if (previousExists && previousBytes is not null) File.WriteAllBytes(source, previousBytes);
        else if (File.Exists(source)) File.Delete(source);
    }
}

async Task ForceInstallScenario()
{
    var baseDir = AppContext.BaseDirectory;
    var package = Path.Combine(baseDir, "content", "3.8.11", "WuWaVH_PC_R29_99_P.pak");
    var releaseMarker = Path.Combine(baseDir, "content", "r29-release.json");
    var packageExisted = File.Exists(package);
    var markerExisted = File.Exists(releaseMarker);
    byte[]? oldPackage = packageExisted ? File.ReadAllBytes(package) : null;
    byte[]? oldMarker = markerExisted ? File.ReadAllBytes(releaseMarker) : null;

    var root = Path.Combine(work, "force-install");
    var game = Path.Combine(root, "Wuthering Waves Game");
    var exe = Path.Combine(game, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
    var resources = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0");
    var officialMount = Path.Combine(resources, "Mount", "MountLang_en.txt");
    var stockSig = Path.Combine(resources, "Lang_en", "3.8.11", "pakchunk10-WindowsNoEditor_P.sig");
    var foreignLoader = Path.Combine(game, "Client", "Binaries", "Win64", "wuwaVietHoa", "plugin.dll");
    var foreignSidecar = Path.Combine(resources, "Mount", "othertranslation.txt");
    var foreignPak = Path.Combine(resources, "othertranslation", "Text_99_P.pak");
    var foreignSig = Path.Combine(resources, "othertranslation", "Text_99_P.sig");

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(package)!);
        File.Copy(realPak, package, overwrite: true);
        File.WriteAllText(releaseMarker, "{\"translationRelease\":\"R29\"}");

        Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
        File.WriteAllBytes(exe, new byte[] { 4, 3, 2, 1 });
        Directory.CreateDirectory(Path.GetDirectoryName(officialMount)!);
        File.WriteAllText(officialMount, "::Mount::\r\n");
        Directory.CreateDirectory(Path.GetDirectoryName(stockSig)!);
        File.WriteAllBytes(stockSig, new byte[] { 1, 2, 3, 4 });
        var exeBefore = SHA256.HashData(File.ReadAllBytes(exe));
        var mountBefore = SHA256.HashData(File.ReadAllBytes(officialMount));
        var sigBefore = SHA256.HashData(File.ReadAllBytes(stockSig));

        Directory.CreateDirectory(Path.GetDirectoryName(foreignLoader)!);
        File.WriteAllBytes(foreignLoader, new byte[] { 7, 7, 7 });
        Directory.CreateDirectory(Path.GetDirectoryName(foreignPak)!);
        File.WriteAllBytes(foreignPak, new byte[] { 8, 8, 8 });
        File.WriteAllBytes(foreignSig, new byte[] { 9, 9 });
        File.WriteAllText(foreignSidecar,
            "::Mount::\r\nothertranslation/Text_99_P,99,abc,def,,\r\n::Del::\r\n");
        File.SetAttributes(foreignLoader, FileAttributes.ReadOnly);
        File.SetAttributes(foreignPak, FileAttributes.ReadOnly);

        var installer = new ViethoaInstaller(new NullLog(), quarantineRoot: Path.Combine(work, "quarantine-force"), isGameRunning: () => false);
        // Active, unverified sidecar is not proof of WAHU ownership; fail closed.
        var blocked = await installer.InstallAsync(game, NameVariant.English, withFont: false);
        if (blocked.Success || R29MountedPak.IsInstalled(game))
            throw new Exception("Unknown language Mount was allowed");
        if (!File.Exists(foreignLoader) || !File.Exists(foreignSidecar)
            || !File.Exists(foreignPak) || !File.Exists(foreignSig))
            throw new Exception("Unknown language Mount preflight mutated game files");
        Console.WriteLine("PASS R29_UNKNOWN_MOUNT_BLOCKED_AND_PRESERVED=true");

        // Fixture-only: user explicitly removes the unidentified sidecar.
        // Unreferenced, unrelated pak/sig must still be preserved.
        File.Delete(foreignSidecar);
        var result = await installer.InstallAsync(game, NameVariant.English, withFont: false);
        if (!result.Success) throw new Exception("Force install failed: " + result.Error);
        if (!R29MountedPak.IsInstalled(game)) throw new Exception("Force install did not activate R29");
        if (File.Exists(foreignLoader))
            throw new Exception("Known conflicting Win64 loader was not quarantined");
        if (!File.Exists(foreignPak) || !File.Exists(foreignSig))
            throw new Exception("Unreferenced foreign pak/sig were deleted without provenance");
        if (installer.LastAutoDeletedConflictCount < 1)
            throw new Exception("Force install did not quarantine known conflicts");
        if (string.IsNullOrWhiteSpace(installer.LastQuarantineDirectory)
            || !File.Exists(Path.Combine(installer.LastQuarantineDirectory, "quarantine-manifest.json"))
            || Directory.GetFiles(installer.LastQuarantineDirectory, "*", SearchOption.AllDirectories).Length < 2)
            throw new Exception("R29 quarantine did not preserve known loader backups");
        Console.WriteLine("PASS R29_QUARANTINE_HAS_SHA_MANIFEST=true");
        if (!SHA256.HashData(File.ReadAllBytes(exe)).SequenceEqual(exeBefore))
            throw new Exception("Stock EXE changed during force install");
        if (!SHA256.HashData(File.ReadAllBytes(officialMount)).SequenceEqual(mountBefore))
            throw new Exception("Official MountLang_en changed during force install");
        if (!SHA256.HashData(File.ReadAllBytes(stockSig)).SequenceEqual(sigBefore))
            throw new Exception("Stock English SIG changed during force install");

        Console.WriteLine("PASS R29_FORCE_INSTALL_CLEANS_CONFLICTS_PRESERVES_STOCK=true");
    }
    finally
    {
        try
        {
            if (packageExisted && oldPackage is not null) File.WriteAllBytes(package, oldPackage);
            else if (File.Exists(package)) File.Delete(package);
            if (markerExisted && oldMarker is not null) File.WriteAllBytes(releaseMarker, oldMarker);
            else if (File.Exists(releaseMarker)) File.Delete(releaseMarker);
        }
        catch { }
    }
}

try
{
    Scenario("base", officialMount: false, englishSig: true, staleWahu: false);
    Scenario("versioned", officialMount: true, englishSig: true, staleWahu: false);
    Scenario("steam-ja", officialMount: true, englishSig: false, staleWahu: false, japaneseSig: true);
    Scenario("nosig", officialMount: false, englishSig: false, staleWahu: false);
    Scenario("nosig-stale", officialMount: false, englishSig: false, staleWahu: true);
    Scenario("steam-ja-tampered", officialMount: true, englishSig: false, staleWahu: false,
        japaneseSig: true, tamperJapaneseMount: true);
    Scenario("steam-ja-unreferenced", officialMount: true, englishSig: false, staleWahu: false,
        japaneseSig: true, omitJapaneseMount: true);
    Scenario("overwrite", officialMount: false, englishSig: true, staleWahu: true);
    await InvalidPackagePreflightScenario();
    await ForceInstallScenario();
    Console.WriteLine("R29_REAL_PAK_FILESYSTEM_SMOKE=PASS");
}
finally
{
    if (Directory.Exists(work))
    {
        foreach (var file in Directory.EnumerateFiles(work, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        foreach (var dir in Directory.EnumerateDirectories(work, "*", SearchOption.AllDirectories))
            File.SetAttributes(dir, FileAttributes.Directory);
        Directory.Delete(work, recursive: true);
    }
}

sealed class NullLog : ILogService
{
    public string LogDirectory => Path.GetTempPath();
    public void Info(string operation, string message) { }
    public void Warn(string operation, string message) { }
    public void Error(string operation, string message, Exception? ex = null) { }
    public IReadOnlyList<LogEntry> ReadRecent(int max = 500, string? levelFilter = null,
        string? search = null) => Array.Empty<LogEntry>();
    public void Clear() { }
}
