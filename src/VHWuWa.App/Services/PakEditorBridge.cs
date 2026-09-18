using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VHWuWa.App.Services;

/// <summary>Gọi backend PAK tách tiến trình bằng request/response JSON.</summary>
public sealed class PakEditorBridge
{
    public string EditorDirectory => Path.Combine(AppContext.BaseDirectory, "editor");
    public string AvatarDirectory => Path.Combine(EditorDirectory, "avatars");
    private string ExecutablePath => Path.Combine(EditorDirectory, "WahuPakEditor.exe");

    public async Task<JsonObject> RequestAsync(object request, CancellationToken ct = default)
    {
        if (!File.Exists(ExecutablePath))
            throw new FileNotFoundException(
                "Bản cài này chưa có bộ sửa PAK. Hãy tải đúng gói VietHoa-WuWa mới nhất từ GitHub Releases.",
                ExecutablePath);

        var temp = Path.Combine(Path.GetTempPath(), "VHWuWa_Editor_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            await File.WriteAllTextAsync(temp, json, new UTF8Encoding(false), ct);

            var start = new ProcessStartInfo
            {
                FileName = ExecutablePath,
                WorkingDirectory = EditorDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            start.ArgumentList.Add("--request");
            start.ArgumentList.Add(temp);
            start.Environment["WAHU_APP_CONTENT"] = Path.Combine(AppContext.BaseDirectory, "content");

            using var process = Process.Start(start)
                ?? throw new InvalidOperationException("Không khởi động được bộ sửa PAK.");
            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            JsonObject? result = null;
            try { result = JsonNode.Parse(stdout) as JsonObject; }
            catch (JsonException) { }
            if (result is null)
                throw new InvalidDataException("Backend trả dữ liệu không hợp lệ. " + stderr.Trim());
            if (result["ok"]?.GetValue<bool>() != true)
            {
                var message = result["error"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(message) && result["failures"] is JsonArray failures)
                {
                    message = string.Join("\n", failures.OfType<JsonObject>().Select(f =>
                        $"{f["cn"]} [{f["mode"]}]: {f["error"]}"));
                }
                throw new InvalidOperationException(message ?? stderr.Trim() ?? "Bộ sửa PAK báo lỗi.");
            }
            return result;
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { }
        }
    }
}
