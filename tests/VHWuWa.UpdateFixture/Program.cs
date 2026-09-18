using System.Reflection;

namespace VHWuWa.UpdateFixture;

/// <summary>App giả chỉ dùng cho test end-to-end của updater; không đóng gói vào release.</summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        var options = Parse(args);
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "disable-health.flag")))
            return 23;

        if (!options.TryGetValue("update-health-file", out var healthFile)
            || !options.TryGetValue("update-expected-version", out var expectedVersion))
            return 0;

        var actual = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? string.Empty;
        if (!string.Equals(actual, NormalizeVersion(expectedVersion), StringComparison.OrdinalIgnoreCase))
            return 24;

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(healthFile))!);
        File.WriteAllText(healthFile, actual);
        Thread.Sleep(4_000);
        return 0;
    }

    private static string NormalizeVersion(string value)
    {
        if (!Version.TryParse(value, out var version)) return value.Trim();
        return new Version(version.Major, Math.Max(0, version.Minor), Math.Max(0, version.Build)).ToString();
    }

    private static Dictionary<string, string> Parse(string[] args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) continue;
            var key = args[i][2..];
            result[key] = i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal)
                ? args[++i]
                : "true";
        }
        return result;
    }
}
