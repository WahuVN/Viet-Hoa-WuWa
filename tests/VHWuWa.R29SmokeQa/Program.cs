using System.Security.Cryptography;
using VHWuWa.Core.Abstractions;
using VHWuWa.Core.Models;
using VHWuWa.Infrastructure;

if (args.Length is < 1 or > 2 || (args.Length == 2 && args[1] != "--auto-conflicts-only"))
    throw new ArgumentException("Usage: VHWuWa.R29SmokeQa <real-R29-pak> [--auto-conflicts-only]");
var onlyAutoConflicts = args.Length == 2;
var realPak = Path.GetFullPath(args[0]);
const string expected = "9f3503f4b7d742425ee27b20e220a1aa93c758f72f4ee25cd67e387682d3e1a3";
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
    var sig = Path.Combine(res, "Lang_en", mode == "base" ? "Base" : "3.8.13",
        "pakchunk10-WindowsNoEditor_P.sig");
    Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
    File.WriteAllBytes(exe, new byte[] { 1, 3, 3, 7 });
    if (englishSig)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(sig)!);
        File.WriteAllBytes(sig, new byte[] { 10, 20, 30, 40, 50 });
    }
    var japaneseStockSig = Path.Combine(res, "Lang_ja", "3.8.13",
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
        File.WriteAllText(japaneseMount, "::Mount::\r\nLang_ja/3.8.13/pakchunk10-WindowsNoEditor_P,10,PAKSHA,"
            + officialSha + ",,\r\n::Del::\r\n");
        originalJapaneseMount = File.ReadAllBytes(japaneseMount);
    }

    var sidecar = Path.Combine(res, "Mount", "Vietnamese.txt");
    var dest = Path.Combine(res, "Vietnamese", "Vietnamese_99_P.pak");
    var destSig = Path.Combine(res, "Vietnamese", "Vietnamese_99_P.sig");
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

    var package = Path.Combine(app, "content", "3.8.13", "Vietnamese_R29_99_P.pak");
    Directory.CreateDirectory(Path.GetDirectoryName(package)!);
    File.Copy(realPak, package, overwrite: false);

    if (!R29MountedPak.PackageVerified(app))
        throw new Exception("Package SHA-256 preflight failed for " + mode);
    var expectedCompatible = englishSig || (japaneseSig && !tamperJapaneseMount && !omitJapaneseMount);
    var sigAvailable = R29MountedPak.StockSig(game) is not null;
    if (sigAvailable != expectedCompatible)
        throw new Exception("SIG source preflight mismatch for " + mode);
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
    // Changed or unverified files at the WAHU destination are now user-owned
    // until provenance is proven; a stale sidecar must not be overwritten.
    if (staleWahu)
    {
        if (result.Success) throw new Exception("Unverified R29 files were overwritten: " + mode);
        if (oldPak is null || oldSig is null || oldSidecar is null
            || !File.ReadAllBytes(dest).SequenceEqual(oldPak)
            || !File.ReadAllBytes(destSig).SequenceEqual(oldSig)
            || !File.ReadAllBytes(sidecar).SequenceEqual(oldSidecar))
            throw new Exception("Unverified R29 preimages were modified: " + mode);
        Console.WriteLine("PASS R29_STALE_UNVERIFIED_FILES_PRESERVED=" + mode);
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
    var source = Path.Combine(AppContext.BaseDirectory, "content", "3.8.13", "Vietnamese_R29_99_P.pak");
    var previousExists = File.Exists(source);
    var previousBytes = previousExists ? File.ReadAllBytes(source) : null;
    var game = Path.Combine(work, "invalid-r29", "game");
    var exe = Path.Combine(game, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
    var mount = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0", "Mount", "Vietnamese.txt");
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
    var package = Path.Combine(baseDir, "content", "3.8.13", "Vietnamese_R29_99_P.pak");
    var releaseMarker = Path.Combine(baseDir, "content", "r29-release.json");
    var fontDestination = Path.Combine(baseDir, "content", "font", "VietnameseFonts_B119_100_P.pak");
    var fontExisted = File.Exists(fontDestination);
    var oldFont = fontExisted ? File.ReadAllBytes(fontDestination) : null;
    var ancestor = new DirectoryInfo(baseDir);
    string? realFont = null;
    while (ancestor is not null)
    {
        var candidate = Path.Combine(ancestor.FullName, "content", "font", "VietnameseFonts_B119_100_P.pak");
        if (File.Exists(candidate) && !candidate.Equals(fontDestination, StringComparison.OrdinalIgnoreCase))
        {
            realFont = candidate;
            break;
        }
        ancestor = ancestor.Parent;
    }
    if (realFont is null) throw new FileNotFoundException("Missing packaged B119 multi-font QA package");
    var packageExisted = File.Exists(package);
    var markerExisted = File.Exists(releaseMarker);
    byte[]? oldPackage = packageExisted ? File.ReadAllBytes(package) : null;
    byte[]? oldMarker = markerExisted ? File.ReadAllBytes(releaseMarker) : null;

    var root = Path.Combine(work, "force-install");
    var game = Path.Combine(root, "Wuthering Waves Game");
    var exe = Path.Combine(game, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
    var resources = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0");
    var officialMount = Path.Combine(resources, "Mount", "MountLang_en.txt");
    var stockSig = Path.Combine(resources, "Lang_en", "3.8.13", "pakchunk10-WindowsNoEditor_P.sig");
    var foreignLoader = Path.Combine(game, "Client", "Binaries", "Win64", "wuwaVietHoa", "plugin.dll");
    var foreignSidecar = Path.Combine(resources, "Mount", "othertranslation.txt");
    var foreignPak = Path.Combine(resources, "othertranslation", "Text_99_P.pak");
    var foreignSig = Path.Combine(resources, "othertranslation", "Text_99_P.sig");

    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(package)!);
        Directory.CreateDirectory(Path.GetDirectoryName(fontDestination)!);
        File.Copy(realFont, fontDestination, overwrite: true);
        File.Copy(realPak, package, overwrite: true);
        File.WriteAllText(releaseMarker, "{\"translationRelease\":\"R29\"}");

        Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
        File.WriteAllBytes(exe, new byte[] { 4, 3, 2, 1 });
        Directory.CreateDirectory(Path.GetDirectoryName(officialMount)!);
        File.WriteAllText(officialMount, "::Mount::\r\n");
        var optionMount = Path.Combine(resources, "Mount", "MountOption_Pack_LY_UHD.txt");
        var launcherMount = Path.Combine(resources, "Mount", "MountLauncher.txt");
        File.WriteAllText(optionMount, "::Mount::\r\nstock/option\r\n");
        File.WriteAllText(launcherMount, "::Mount::\r\nstock/launcher\r\n");
        var optionBefore = SHA256.HashData(File.ReadAllBytes(optionMount));
        var launcherBefore = SHA256.HashData(File.ReadAllBytes(launcherMount));
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
        // Old WAHU R28/3.7 files are obsolete and must be deleted permanently.
        // Unrelated third-party conflicts remain recoverable through quarantine.
        var oldMods = Path.Combine(game, "Client", "Content", "Paks", "~WuWaMods");
        Directory.CreateDirectory(oldMods);
        var legacyPak = Path.Combine(oldMods, "WuWaVH_99_P.pak");
        var legacySig = Path.Combine(oldMods, "WuWaVH_99_P.sig");
        var legacyFont = Path.Combine(oldMods, "WahuFont_100_P.pak");
        var legacyFontSig = Path.Combine(oldMods, "WahuFont_100_P.sig");
        var oldWin64Loader = Path.Combine(game, "Client", "Binaries", "Win64", "version.dll");
        var oldPublicText = Path.Combine(resources, "wahuviethoa");
        var oldPublicFont = Path.Combine(resources, "wahufont");
        Directory.CreateDirectory(oldPublicText);
        Directory.CreateDirectory(oldPublicFont);
        var oldPublicTextPak = Path.Combine(oldPublicText, "WuWaVH_99_P.pak");
        var oldPublicTextSig = Path.Combine(oldPublicText, "WuWaVH_99_P.sig");
        var oldPublicTextMount = Path.Combine(resources, "Mount", "wahuviethoa.txt");
        var oldPublicFontPak = Path.Combine(oldPublicFont, "WahuFont_100_P.pak");
        var oldPublicFontSig = Path.Combine(oldPublicFont, "WahuFont_100_P.sig");
        var oldPublicFontReceipt = Path.Combine(oldPublicFont, "font-receipt.json");
        var oldPublicFontMount = Path.Combine(resources, "Mount", "wahufont.txt");
        var legacy = new[] { legacyPak, legacySig, legacyFont, legacyFontSig, oldWin64Loader,
            oldPublicTextPak, oldPublicTextSig, oldPublicTextMount, oldPublicFontPak, oldPublicFontSig,
            oldPublicFontReceipt, oldPublicFontMount };
        var foreign = new[] { foreignLoader, foreignPak, foreignSig, foreignSidecar };
        var foreignHashes = new Dictionary<string, string>();
        foreach (var file in legacy)
            if (!File.Exists(file)) File.WriteAllText(file, "LEGACY_WAHU_MOD");
        foreach (var file in foreign)
        {
            if (!File.Exists(file)) File.WriteAllText(file, "USER_FOREIGN_MOD");
            foreignHashes[file] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
        }

        var result = await installer.InstallAsync(game, NameVariant.English, withFont: false);
        if (!result.Success) throw new Exception("Automatic conflict cleanup failed: " + result.Error);
        if (!R29MountedPak.IsInstalled(game)) throw new Exception("Automatic cleanup failed to activate R29");
        if (installer.LastAutoDeletedConflictCount < legacy.Length + foreign.Length)
            throw new Exception("Not all old WAHU and conflicting user mods were cleared");

        foreach (var file in legacy)
            if (File.Exists(file))
                throw new Exception("Old WAHU file was not permanently deleted: " + file);

        var backup = installer.LastQuarantineDirectory;
        if (backup is null || !File.Exists(Path.Combine(backup, "quarantine-manifest.json")))
            throw new Exception("Missing automatic SHA-verified backup for third-party conflicts");
        foreach (var (file, hash) in foreignHashes)
        {
            if (File.Exists(file)) throw new Exception("Conflicting third-party mod still active: " + file);
            var saved = Path.Combine(backup, Path.GetRelativePath(game, file));
            if (!File.Exists(saved)
                || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(saved))) != hash)
                throw new Exception("Missing or altered backup of third-party mod: " + file);
        }
        foreach (var file in legacy)
        {
            var saved = Path.Combine(backup, Path.GetRelativePath(game, file));
            if (File.Exists(saved))
                throw new Exception("Old WAHU file was quarantined instead of permanently deleted: " + file);
        }
        Console.WriteLine("PASS R29_PERMANENTLY_DELETES_OLD_WAHU_AND_QUARANTINES_EXTERNAL="
            + legacy.Length + "/" + foreign.Length);

        if (!SHA256.HashData(File.ReadAllBytes(exe)).SequenceEqual(exeBefore))
            throw new Exception("Stock EXE changed during force install");
        if (!SHA256.HashData(File.ReadAllBytes(officialMount)).SequenceEqual(mountBefore))
            throw new Exception("Official MountLang_en changed during force install");
        if (!File.Exists(optionMount) || !SHA256.HashData(File.ReadAllBytes(optionMount)).SequenceEqual(optionBefore))
            throw new Exception("Official MountOption_Pack was removed or altered by auto cleanup");
        if (!File.Exists(launcherMount) || !SHA256.HashData(File.ReadAllBytes(launcherMount)).SequenceEqual(launcherBefore))
            throw new Exception("Official MountLauncher was removed or altered by auto cleanup");
        if (!SHA256.HashData(File.ReadAllBytes(stockSig)).SequenceEqual(sigBefore))
            throw new Exception("Stock English SIG changed during force install");

        Console.WriteLine("PASS R29_FORCE_INSTALL_CLEANS_CONFLICTS_PRESERVES_STOCK=true");

        var blockedRestore = installer.RestoreQuarantine(game, backup);
        if (blockedRestore.Success || foreign.Any(File.Exists))
            throw new Exception("Third-party mods were restored while R29 remained mounted");
        if (legacy.Any(File.Exists))
            throw new Exception("Old WAHU files unexpectedly returned while R29 remained mounted");
        Console.WriteLine("PASS R29_BLOCK_RESTORE_EXTERNAL_MODS_WHILE_ACTIVE=true");

        // No file in an active R29 installation may be touched while the
        // game/launcher is running, even if no third-party conflicts remain.
        var whileRunning = new ViethoaInstaller(new NullLog(),
            quarantineRoot: Path.Combine(work, "quarantine-running"),
            isGameRunning: () => true);
        var reinstallWhileRunning = await whileRunning.InstallAsync(game, NameVariant.English, withFont: false);
        if (reinstallWhileRunning.Success)
            throw new Exception("R29 install modified an active running game");
        var uninstallWhileRunning = await whileRunning.UninstallAsync(game);
        if (uninstallWhileRunning.Success || !R29MountedPak.IsInstalled(game))
            throw new Exception("R29 uninstall modified an active running game");
        Console.WriteLine("PASS R29_RUNNING_GAME_INSTALL_AND_UNINSTALL_FAIL_CLOSED=true");

        var uninstalled = await installer.UninstallAsync(game);
        if (!uninstalled.Success)
            throw new Exception("R29 uninstall before restoring external mods failed: " + uninstalled.Error);
        var recovered = installer.RestoreQuarantine(game, backup);
        if (!recovered.Success || recovered.Value != foreign.Length)
            throw new Exception("Restore external mods after uninstall failed: " + recovered.Error);
        foreach (var (file, hash) in foreignHashes)
            if (!File.Exists(file)
                || Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))) != hash)
                throw new Exception("Restored external mod SHA does not match original: " + file);
        if (legacy.Any(File.Exists))
            throw new Exception("Old WAHU files were restored after uninstall");
        Console.WriteLine("PASS R29_UNINSTALL_RESTORES_ONLY_EXTERNAL_MODS_EXACT_SHA=" + foreign.Length);

        // A broken font directory must not leave the user's old mod disabled
        // after the one-click R29 install fails.
        var failedRoot = Path.Combine(work, "r29-font-failure-rollback");
        var failedGame = Path.Combine(failedRoot, "Wuthering Waves Game");
        var failedResources = Path.Combine(failedGame, "Client", "Saved", "Resources", "3.8.0");
        var failedExe = Path.Combine(failedGame, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
        var failedSig = Path.Combine(failedResources, "Lang_en", "Base",
            "pakchunk10-WindowsNoEditor_P.sig");
        var failedOldMod = Path.Combine(failedGame, "Client", "Content", "Paks",
            "~mods", "OlderVisual_99_P.pak");
        var failedFontDir = Path.Combine(failedResources, "VietnameseFonts");
        foreach (var file in new[] { failedExe, failedSig, failedOldMod, failedFontDir })
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllBytes(failedExe, new byte[] { 1, 3, 3, 7 });
        File.WriteAllBytes(failedSig, new byte[] { 10, 20, 30, 40 });
        File.WriteAllText(failedOldMod, "USER_PREEXISTING_MOD");
        File.WriteAllText(failedFontDir, "BLOCK_FONT_DIRECTORY");
        var oldModHash = SHA256.HashData(File.ReadAllBytes(failedOldMod));
        var failingInstaller = new ViethoaInstaller(new NullLog(),
            quarantineRoot: Path.Combine(work, "quarantine-font-fail"),
            isGameRunning: () => false);
        var failedInstall = await failingInstaller.InstallAsync(failedGame, NameVariant.English, withFont: false);
        if (failedInstall.Success || R29MountedPak.IsInstalled(failedGame)
            || R29FontMountedPak.IsInstalled(failedGame)
            || !File.Exists(failedOldMod)
            || !SHA256.HashData(File.ReadAllBytes(failedOldMod)).SequenceEqual(oldModHash)
            || !failedInstall.Error!.Contains("tự khôi phục", StringComparison.OrdinalIgnoreCase))
            throw new Exception("Failed one-click R29 install did not restore previous mod exactly");
        var failedBackup = failingInstaller.LastQuarantineDirectory;
        if (failedBackup is null || !File.Exists(Path.Combine(failedBackup, "quarantine-manifest.json"))
            || !File.Exists(Path.Combine(failedBackup, Path.GetRelativePath(failedGame, failedOldMod))))
            throw new Exception("Failed one-click R29 install discarded recoverable backup");
        Console.WriteLine("PASS R29_FONT_FAIL_AUTO_RESTORES_OLD_MOD_SHA=true");
    }
    finally
    {
        try
        {
            if (packageExisted && oldPackage is not null) File.WriteAllBytes(package, oldPackage);
            else if (File.Exists(package)) File.Delete(package);
            if (markerExisted && oldMarker is not null) File.WriteAllBytes(releaseMarker, oldMarker);
            else if (File.Exists(releaseMarker)) File.Delete(releaseMarker);
            if (fontExisted && oldFont is not null) File.WriteAllBytes(fontDestination, oldFont);
            else if (File.Exists(fontDestination)) File.Delete(fontDestination);
        }
        catch { }
    }
}

try
{
    if (onlyAutoConflicts)
    {
        await ForceInstallScenario();
        Console.WriteLine("R29_AUTOMATIC_ALL_CONFLICTING_MODS_SMOKE=PASS");
    }
    else
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
