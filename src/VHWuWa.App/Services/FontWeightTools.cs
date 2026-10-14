using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace VHWuWa.App.Services;

/// <summary>Only true variable fonts have a continuous weight axis; do not simulate bold on static fonts.</summary>
public static class FontWeightTools
{
    public sealed record WeightInfo(bool IsVariable, int Minimum, int Maximum, int Default)
    {
        public static readonly WeightInfo Unknown = new(false, 400, 400, 400);
    }

    private static ushort U16(byte[] b, int i) => (ushort)((b[i] << 8) | b[i + 1]);
    private static uint U32(byte[] b, int i) => ((uint)b[i] << 24) | ((uint)b[i + 1] << 16) | ((uint)b[i + 2] << 8) | b[i + 3];
    private static double Fixed(byte[] b, int i) => unchecked((int)U32(b, i)) / 65536.0;

    public static WeightInfo Inspect(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return WeightInfo.Unknown;
        try
        {
            var data = File.ReadAllBytes(path);
            if (data.Length < 12) return WeightInfo.Unknown;
            // Collection fonts can contain several unrelated faces: no continuous slider.
            if (data[0] == 't' && data[1] == 't' && data[2] == 'c' && data[3] == 'f')
                return WeightInfo.Unknown;
            var count = U16(data, 4);
            var defaultWeight = 400;
            WeightInfo? variable = null;
            for (int n = 0; n < count && 12 + n * 16 + 16 <= data.Length; n++)
            {
                int entry = 12 + n * 16;
                var tag = Encoding.ASCII.GetString(data, entry, 4);
                long offset = U32(data, entry + 8);
                long length = U32(data, entry + 12);
                if (offset < 0 || offset + length > data.Length || length < 0) continue;
                int o = (int)offset;
                if (tag == "OS/2" && length >= 6)
                    defaultWeight = Math.Clamp((int)U16(data, o + 4), 1, 1000);
                if (tag == "fvar" && length >= 16)
                {
                    int axesOffset = U16(data, o + 4);
                    int axisCount = U16(data, o + 8);
                    int axisSize = U16(data, o + 10);
                    if (axisSize < 20 || axisCount > 64) continue;
                    for (int a = 0; a < axisCount; a++)
                    {
                        long ax = (long)o + axesOffset + (long)a * axisSize;
                        if (ax + 20 > o + length) break;
                        int axis = (int)ax;
                        if (Encoding.ASCII.GetString(data, axis, 4) != "wght") continue;
                        int min = (int)Math.Ceiling(Fixed(data, axis + 4));
                        int max = (int)Math.Floor(Fixed(data, axis + 12));
                        int def = (int)Math.Round(Fixed(data, axis + 8));
                        if (max < min || min < 1 || max > 1000) continue;
                        variable = new WeightInfo(true, min, max, Math.Clamp(def, min, max));
                        break;
                    }
                }
            }
            return variable ?? new WeightInfo(false, defaultWeight, defaultWeight, defaultWeight);
        }
        catch { return WeightInfo.Unknown; }
    }

    public static async Task<string> InstantiateAsync(string fontPath, int weight, CancellationToken ct = default)
    {
        var info = Inspect(fontPath);
        if (!info.IsVariable) throw new InvalidOperationException("Font tĩnh không hỗ trợ chỉnh weight liên tục.");
        if (weight < info.Minimum || weight > info.Maximum)
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight nằm ngoài trục hỗ trợ của font.");

        var scriptCandidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "tools", "font-weight-instance.py"),
            Path.Combine(AppContext.BaseDirectory, "scripts", "font-weight-instance.py")
        };
        var script = scriptCandidates.FirstOrDefault(File.Exists);
        var packagedExe = Path.Combine(AppContext.BaseDirectory, "tools", "font-weight-instance.exe");

        var fileInfo = new FileInfo(fontPath);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            Path.GetFullPath(fontPath) + ":" + fileInfo.Length + ":" + fileInfo.LastWriteTimeUtc.Ticks + ":" + weight)))[..20];
        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VHWuWa", "FontInstances");
        Directory.CreateDirectory(cacheDir);
        var output = Path.Combine(cacheDir, $"{Path.GetFileNameWithoutExtension(fontPath)}_{weight}_{hash}.ttf");
        if (File.Exists(output) && new FileInfo(output).Length > 128)
            return output;

        // Public releases carry a self-contained compiler: players need no Python.
        if (File.Exists(packagedExe))
        {
            var psi = new ProcessStartInfo(packagedExe)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            psi.ArgumentList.Add(fontPath);
            psi.ArgumentList.Add(output);
            psi.ArgumentList.Add(weight.ToString(System.Globalization.CultureInfo.InvariantCulture));
            using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Không chạy được bộ tạo font.");
            var outTask = proc.StandardOutput.ReadToEndAsync(ct);
            var errTask = proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);
            var outText = await outTask;
            var errText = await errTask;
            if (proc.ExitCode == 0 && File.Exists(output) && new FileInfo(output).Length > 128)
                return output;
            throw new InvalidOperationException("Bộ tạo font trả về lỗi: " +
                (string.IsNullOrWhiteSpace(errText) ? outText : errText).Trim());
        }
        if (script is null)
            throw new FileNotFoundException("Thiếu bộ tạo font biến thiên (tools/font-weight-instance.exe).");
        string[] interpreters = ["py", "python"];
        string lastError = "";
        foreach (var interpreter in interpreters)
        {
            try
            {
                var psi = new ProcessStartInfo(interpreter)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                if (interpreter == "py") psi.ArgumentList.Add("-3");
                psi.ArgumentList.Add(script);
                psi.ArgumentList.Add(fontPath);
                psi.ArgumentList.Add(output);
                psi.ArgumentList.Add(weight.ToString(System.Globalization.CultureInfo.InvariantCulture));
                using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Không chạy được Python.");
                var stdout = proc.StandardOutput.ReadToEndAsync(ct);
                var stderr = proc.StandardError.ReadToEndAsync(ct);
                await proc.WaitForExitAsync(ct);
                var outText = await stdout;
                var errorText = await stderr;
                if (proc.ExitCode == 0 && File.Exists(output) && new FileInfo(output).Length > 128)
                    return output;
                lastError = string.IsNullOrWhiteSpace(errorText) ? outText : errorText;
                if (lastError.Contains("No module named", StringComparison.OrdinalIgnoreCase))
                    break;
            }
            catch (System.ComponentModel.Win32Exception ex) { lastError = ex.Message; }
        }
        throw new InvalidOperationException("Không tạo được font weight " + weight +
            ". Cần Python 3 và fontTools (py -m pip install fonttools). " + lastError.Trim());
    }
}
