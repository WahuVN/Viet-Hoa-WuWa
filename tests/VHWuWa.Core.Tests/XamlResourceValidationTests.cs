using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace VHWuWa.Core.Tests;

public class XamlResourceValidationTests
{
    [Fact]
    public void AllStaticResourceReferences_InAllXamlFiles_MustBeDeclared()
    {
        var current = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(current);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "VHWuWa.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var appDir = Path.Combine(dir.FullName, "src", "VHWuWa.App");
        Assert.True(Directory.Exists(appDir), $"Thu muc App khong ton tai: {appDir}");

        var appXamlFile = Path.Combine(appDir, "App.xaml");
        Assert.True(File.Exists(appXamlFile), "Khong tim thay App.xaml");

        var appXamlContent = File.ReadAllText(appXamlFile);
        var appKeyMatches = Regex.Matches(appXamlContent, @"x:Key=""([^""]+)""");
        var globalKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in appKeyMatches)
        {
            globalKeys.Add(m.Groups[1].Value);
        }

        // App-level merged dictionaries are available to every XAML page at runtime.
        // Verify their actual source files and include their declared resources.
        var mergedDictionarySources = Regex.Matches(
            appXamlContent,
            @"<ResourceDictionary\s+Source=""([^""]+)""",
            RegexOptions.IgnoreCase);
        foreach (Match source in mergedDictionarySources)
        {
            var resourcePath = source.Groups[1].Value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var absolutePath = Path.GetFullPath(Path.Combine(appDir, resourcePath));
            Assert.True(
                absolutePath.StartsWith(Path.GetFullPath(appDir) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                $"ResourceDictionary path outside App: {absolutePath}");
            Assert.True(File.Exists(absolutePath), $"Merged ResourceDictionary does not exist: {absolutePath}");
            foreach (Match key in Regex.Matches(File.ReadAllText(absolutePath), @"x:Key=""([^""]+)"""))
            {
                globalKeys.Add(key.Groups[1].Value);
            }
        }

        var xamlFiles = Directory.GetFiles(appDir, "*.xaml", SearchOption.AllDirectories);
        var errors = new List<string>();

        var staticResRegex = new Regex(@"\{StaticResource\s+([^},]+)", RegexOptions.Compiled);
        var keyRegex = new Regex(@"x:Key=""([^""]+)""", RegexOptions.Compiled);

        foreach (var file in xamlFiles)
        {
            if (Path.GetFileName(file).Equals("App.xaml", StringComparison.OrdinalIgnoreCase))
                continue;

            var content = File.ReadAllText(file);
            var localKeys = new HashSet<string>(globalKeys, StringComparer.Ordinal);
            foreach (Match m in keyRegex.Matches(content))
            {
                localKeys.Add(m.Groups[1].Value);
            }

            var relPath = Path.GetRelativePath(appDir, file);
            foreach (Match m in staticResRegex.Matches(content))
            {
                var refKey = m.Groups[1].Value.Trim();
                if (!localKeys.Contains(refKey))
                {
                    errors.Add($"File [{relPath}] tham chieu StaticResource '{refKey}' nhung khong duoc dinh nghia!");
                }
            }
        }

        Assert.True(errors.Count == 0,
            "PHAT HIEN LOI STATIC RESOURCE TRONG XAML:\n" + string.Join("\n", errors));
    }
}
