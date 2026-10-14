using System.Diagnostics;
using System.Security.Cryptography;
using VHWuWa.Infrastructure;

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: FontPakBuilder <static-font.ttf> <repak.exe> <output.pak>");
    return 2;
}
var font = Path.GetFullPath(args[0]);
var repak = Path.GetFullPath(args[1]);
var output = Path.GetFullPath(args[2]);
if (!File.Exists(font) || !File.Exists(repak))
{
    Console.Error.WriteLine("Missing source TTF or repak.exe.");
    return 3;
}
var dir = Path.Combine(Path.GetTempPath(), "VHWuWaLexendPak_" + Guid.NewGuid().ToString("N"));
try
{
    var content = Path.Combine(dir, "content");
    var fontDir = Path.Combine(content, "Client", "Content", "Aki", "UI", "Framework", "LGUI", "Font");
    Directory.CreateDirectory(fontDir);
    File.Copy(font, Path.Combine(fontDir, "LaguSansBold.ufont"));
    var v11 = Path.Combine(dir, "font.v11.tmp");
    var psi = new ProcessStartInfo(repak)
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };
    foreach (var value in new[] {"pack", "--version", "V11", "--mount-point", "../../../", content, v11})
        psi.ArgumentList.Add(value);
    using var proc = Process.Start(psi) ?? throw new IOException("Could not start repak.");
    var stdout = proc.StandardOutput.ReadToEndAsync();
    var stderr = proc.StandardError.ReadToEndAsync();
    await proc.WaitForExitAsync();
    var info = (await stdout) + " " + (await stderr);
    if (proc.ExitCode != 0 || !File.Exists(v11))
        throw new IOException("repak pack failed: " + info);
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    PakV12Converter.ConvertV11ToV12(v11, output);
    if (!PakV12Converter.TryVerifyV12(output, out var error))
        throw new InvalidDataException("V12 check failed: " + error);
    var sha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(output))).ToLowerInvariant();
    Console.WriteLine($"FONT_PAK_OK file={output} size={new FileInfo(output).Length} sha256={sha}");
    return 0;
}
catch (Exception e)
{
    if (File.Exists(output)) File.Delete(output);
    Console.Error.WriteLine("FONT_PAK_FAIL " + e);
    return 4;
}
finally
{
    try { Directory.Delete(dir, true); } catch { }
}
