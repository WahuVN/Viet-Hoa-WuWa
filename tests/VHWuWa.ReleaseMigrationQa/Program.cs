using VHWuWa.Core.Services;
using VHWuWa.Infrastructure;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: VHWuWa.ReleaseMigrationQa <sandbox-app-dir> <sandbox-backup-dir>");
    return 2;
}

var app = Path.GetFullPath(args[0]);
var backups = Path.GetFullPath(args[1]);
if (!Directory.Exists(app) || !File.Exists(Path.Combine(app, "VHWuWa.exe")))
{
    Console.Error.WriteLine("Missing standalone test application directory.");
    return 3;
}
if (!R29MountedPak.ReleaseRequired(app) || !R29MountedPak.PackageVerified(app))
{
    Console.Error.WriteLine("R29 marker/package is missing or SHA-256 invalid. Refusing migration.");
    return 4;
}
var result = LegacyReleaseAssetMigration.BackupAndDeactivate(app, backups, verifiedR29: true);
Console.WriteLine($"LEGACY_MOVED={result.BackedUp} UNKNOWN_PRESERVED={result.UnknownPreserved} ERRORS={result.Errors.Count}");
foreach (var error in result.Errors) Console.Error.WriteLine(error);
return result.BackedUp == 6 && result.Errors.Count == 0 ? 0 : 5;
