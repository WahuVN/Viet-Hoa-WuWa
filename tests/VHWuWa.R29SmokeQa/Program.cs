using System.Security.Cryptography;
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

void Scenario(string mode, bool officialMount)
{
    var root = Path.Combine(work, mode);
    var game = Path.Combine(root, "Wuthering Waves Game");
    var app = Path.Combine(root, "portable-v320");
    var exe = Path.Combine(game, "Client", "Binaries", "Win64", "Client-Win64-Shipping.exe");
    var res = Path.Combine(game, "Client", "Saved", "Resources", "3.8.0");
    var sig = Path.Combine(res, "Lang_en", mode == "base" ? "Base" : "3.8.11",
        "pakchunk10-WindowsNoEditor_P.sig");
    Directory.CreateDirectory(Path.GetDirectoryName(exe)!);
    Directory.CreateDirectory(Path.GetDirectoryName(sig)!);
    File.WriteAllBytes(exe, new byte[] { 1, 3, 3, 7 });
    File.WriteAllBytes(sig, new byte[] { 10, 20, 30, 40, 50 });
    var originalExeSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(exe)));
    var originalSigSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sig)));
    var stockMount = Path.Combine(res, "Mount", "MountLang_en.txt");
    if (officialMount)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(stockMount)!);
        File.WriteAllText(stockMount, "::Mount::\r\n");
    }
    var originalMount = File.Exists(stockMount) ? File.ReadAllBytes(stockMount) : null;
    var package = Path.Combine(app, "content", "3.8.11", "WuWaVH_PC_R29_99_P.pak");
    Directory.CreateDirectory(Path.GetDirectoryName(package)!);
    File.Copy(realPak, package, overwrite: false);

    if (!R29MountedPak.PackageVerified(app) || !R29MountedPak.GameCompatible(game))
        throw new Exception("Preflight failed for " + mode + ": " + R29MountedPak.CompatibilityIssue(game));
    var result = R29MountedPak.Install(app, game);
    if (!result.Success) throw new Exception("Install failed for " + mode + ": " + result.Error);
    if (!R29MountedPak.IsInstalled(game)) throw new Exception("Installed files not verified");
    var sidecar = Path.Combine(res, "Mount", "wahuviethoa.txt");
    var dest = Path.Combine(res, "wahuviethoa", "WuWaVH_99_P.pak");
    if (!File.Exists(sidecar) || !File.Exists(dest)) throw new Exception("R29 sidecar or pak absent");
    var destSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(dest))).ToLowerInvariant();
    if (destSha != expected) throw new Exception("Staged R29 pak hash mismatch");
    var undo = R29MountedPak.Uninstall(game);
    if (!undo.Success) throw new Exception("Uninstall failed for " + mode + ": " + undo.Error);
    if (File.Exists(sidecar) || File.Exists(dest) || R29MountedPak.IsInstalled(game))
        throw new Exception("R29 files left after uninstall");
    if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(exe))) != originalExeSha)
        throw new Exception("Stock EXE changed");
    if (Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sig))) != originalSigSha)
        throw new Exception("Stock English SIG changed");
    if ((originalMount is null) != (!File.Exists(stockMount))
        || (originalMount is not null && !File.ReadAllBytes(stockMount).SequenceEqual(originalMount)))
        throw new Exception("Official Mount changed");
    Console.WriteLine("PASS R29_INSTALL_UNINSTALL=" + mode
        + " MOUNT_ORIGINAL_PRESERVED=true SIG_PRESERVED=true");
}

try
{
    Scenario("base", officialMount: false);
    Scenario("versioned", officialMount: true);
    Console.WriteLine("R29_REAL_PAK_FILESYSTEM_SMOKE=PASS");
}
finally
{
    if (Directory.Exists(work)) Directory.Delete(work, recursive: true);
}
