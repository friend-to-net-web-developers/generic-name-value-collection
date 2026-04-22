using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace pvm.Helper;

/// <summary>
/// Provides utility methods for handling PHP version strings and retrieving
/// configurations related to PHP installations. This class includes methods
/// for converting between PHP version formats and accessing environment-based
/// or default settings for PHP installations.
/// </summary>
public static partial class PhpVersionHelper
{
    /// <summary>
    /// Retrieves the root directory path for PHP installations.
    /// The method first checks the environment variable "PVM_PHP_ROOT".
    /// If the variable is not set, it falls back to a default path.
    /// </summary>
    /// <returns>
    /// The PHP root directory path as a string. If the "PVM_PHP_ROOT"
    /// environment variable is not set, the default path "C:\php" is returned.
    /// </returns>
    public static string GetPhpRoot() =>
        Environment.GetEnvironmentVariable("PVM_PHP_ROOT") ?? @"C:\php";

    /// <summary>
    /// Converts a PHP version string into a simplified slug format.
    /// The method removes period characters (".") from the version string.
    /// </summary>
    /// <param name="version">
    /// The PHP version string to convert, typically in a "major.minor.patch" format.
    /// </param>
    /// <returns>
    /// A string representing the version in a slug format without periods.
    /// For example, "7.4.3" would be converted to "743".
    /// </returns>
    public static string ToSlug(string version) =>
        version.Replace(".", "");

    /// <summary>
    /// Converts a version string in slug format into its dotted format representation.
    /// The method inserts a period (".") at the appropriate position
    /// within the slug to reconstruct the version string.
    /// </summary>
    /// <param name="slug">
    /// The version string in slug format, typically a compact form without periods.
    /// For example, "743" or "82".
    /// </param>
    /// <returns>
    /// A string representing the version in dotted format.
    /// For example, "743" would be converted to "7.4.3" and "82" would be converted to "8.2".
    /// </returns>
    public static string ToDotted(string slug) =>
        slug.Length == 2
            ? $"{slug[0]}.{slug[1]}"
            : $"{slug[0]}.{slug[1..]}";

    /// <summary>
    /// Retrieves a list of PHP version slugs installed in the PHP root directory.
    /// </summary>
    /// <returns>An array of installed version slugs (e.g., "84", "85").</returns>
    public static string[] GetInstalledSlugs()
    {
        var phpRoot = GetPhpRoot();
        if (!Directory.Exists(phpRoot)) return [];
        return Directory.GetDirectories(phpRoot, "php*")
            .Select(Path.GetFileName)
            .Where(name => name != null && Regex.IsMatch(name, @"^php\d+$"))
            .Select(name => name![3..])
            .OrderBy(s => s)
            .ToArray();
    }

    /// <summary>
    /// Retrieves the slug of the currently active PHP version.
    /// </summary>
    /// <returns>The active version slug (e.g., "84"), or null if not set.</returns>
    public static string? GetCurrentActiveSlug()
    {
        var phpRoot = GetPhpRoot();
        var activeLink = Path.Combine(phpRoot, "active");
        if (!Directory.Exists(activeLink)) return null;

        var di = new DirectoryInfo(activeLink);
        var target = di.LinkTarget;
        if (target == null) return null;

        return Path.GetFileName(target).Replace("php", "");
    }

    /// <summary>
    /// Indicates the source of a detected PHP version.
    /// </summary>
    public enum DetectionSource
    {
        /// <summary>
        /// Version was detected from an active junction (e.g., C:\php\active).
        /// </summary>
        ActiveJunction,
        /// <summary>
        /// Version was detected from the first php.exe found on the system PATH.
        /// </summary>
        Path
    }

    /// <summary>
    /// Holds information about a detected PHP version.
    /// </summary>
    /// <param name="Slug">The version slug (e.g., "84").</param>
    /// <param name="Source">The detection source (ActiveJunction or Path).</param>
    public record DetectionInfo(string Slug, DetectionSource Source);

    /// <summary>
    /// Attempts to detect the currently used PHP version slug with details about the source.
    /// </summary>
    /// <returns>A DetectionInfo object if a version was detected; otherwise, null.</returns>
    public static DetectionInfo? GetDetectedVersionInfo()
    {
        var active = GetCurrentActiveSlug();
        if (active != null) return new DetectionInfo(active, DetectionSource.ActiveJunction);

        var onPath = FindAllPhpOnPath();
        if (onPath.Count == 0) return null;

        var (exePath, version) = onPath[0];
        var phpRoot = GetPhpRoot();

        // 1. Try to infer from path (e.g. C:\php\php85\php.exe)
        var dir = Path.GetDirectoryName(exePath);
        if (dir != null)
        {
            var parent = Path.GetDirectoryName(dir);
            if (parent != null && parent.Equals(phpRoot, StringComparison.OrdinalIgnoreCase))
            {
                var folderName = Path.GetFileName(dir);
                if (folderName != null && folderName.StartsWith("php", StringComparison.OrdinalIgnoreCase))
                {
                    var slug = folderName[3..];
                    if (Regex.IsMatch(slug, @"^\d+$"))
                    {
                        return new DetectionInfo(slug, DetectionSource.Path);
                    }
                }
            }
        }

        // 2. Try to infer from version string (e.g. "8.5.5" -> "85")
        if (version != null)
        {
            var parts = version.Split('.');
            if (parts.Length >= 2)
            {
                var slug = parts[0] + parts[1];
                if (Directory.Exists(Path.Combine(phpRoot, "php" + slug)))
                {
                    return new DetectionInfo(slug, DetectionSource.Path);
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to detect the currently used PHP version slug.
    /// It first checks for an active junction, then falls back to the first php.exe on PATH
    /// if it belongs to a managed installation.
    /// </summary>
    public static string? GetDetectedSlug() => GetDetectedVersionInfo()?.Slug;

    /// <summary>
    /// Scans the system PATH for php.exe instances.
    /// </summary>
    public static List<(string Path, string? Version)> FindAllPhpOnPath()
    {
        var results = new List<(string Path, string? Version)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var exe = Path.Combine(dir, "php.exe");
            if (File.Exists(exe) && seen.Add(exe))
            {
                var ver = GetVersionFromExe(exe);
                results.Add((exe, ver));
            }
        }
        return results;
    }

    /// <summary>
    /// Extracts the PHP version from the output of a specified executable file.
    /// The method executes the file with the "-v" argument to retrieve version information
    /// and parses the output using a regular expression to identify the version string.
    /// </summary>
    /// <param name="exePath">
    /// The full path to the executable file, typically the PHP binary.
    /// </param>
    /// <returns>
    /// A string containing the PHP version in "major.minor.patch" format if successfully retrieved;
    /// otherwise, null if the file does not exist, the process fails, or the version cannot be parsed.
    /// </returns>
    public static string? GetVersionFromExe(string exePath)
    {
        if (!File.Exists(exePath)) return null;
        try
        {
            var result = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "-v",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            var output = result?.StandardOutput.ReadLine();
            var match = SemanticVersionRegex().Match(output ?? "");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch { return null; }
    }

    private static readonly Dictionary<string, List<string>> _builtInModulesCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Retrieves the list of built-in PHP modules by running 'php -n -m'.
    /// </summary>
    /// <param name="phpDirectory">The directory containing php.exe.</param>
    /// <returns>A list of module names.</returns>
    public static List<string> GetBuiltInModules(string phpDirectory)
    {
        if (_builtInModulesCache.TryGetValue(phpDirectory, out var cached))
        {
            return cached;
        }

        var modules = GetModulesInternal(phpDirectory, "-n -m");
        _builtInModulesCache[phpDirectory] = modules;
        return modules;
    }

    /// <summary>
    /// Retrieves the list of loaded PHP modules (including those from php.ini) by running 'php -m'.
    /// </summary>
    /// <param name="phpDirectory">The directory containing php.exe.</param>
    /// <returns>A list of module names.</returns>
    public static List<string> GetLoadedModules(string phpDirectory)
    {
        return GetModulesInternal(phpDirectory, "-m");
    }

    private static List<string> GetModulesInternal(string phpDirectory, string arguments)
    {
        var exePath = Path.Combine(phpDirectory, "php.exe");
        if (!File.Exists(exePath)) return new List<string>();

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Environment = { ["PHPRC"] = phpDirectory },
                WorkingDirectory = phpDirectory
            });

            if (process == null) return new List<string>();

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(5000))
            {
                process.Kill();
                return new List<string>();
            }

            var output = outputTask.Result;
            var error = errorTask.Result;

            var modules = new List<string>();
            var lines = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            
            if (lines.Count == 0 && !string.IsNullOrWhiteSpace(error))
            {
                lines = error.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                // Skip headers and warnings
                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    continue;
                }
                
                if (line.Contains("PHP Warning:", StringComparison.OrdinalIgnoreCase) || 
                    line.Contains("PHP Notice:", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("PHP Fatal error:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                modules.Add(line);
            }
            
            return modules;
        }
        catch 
        {
            return new List<string>(); 
        }
    }

    [GeneratedRegex(@"PHP (\d+\.\d+\.\d+)")]
    private static partial Regex SemanticVersionRegex();

    /// <summary>
    /// Checks if the specified extensions are loaded using PHP's extension_loaded() function.
    /// This is more accurate for framework requirements as it verifies the engine actually
    /// recognizes and has initialized the extension.
    /// </summary>
    public static Dictionary<string, string> CheckExtensionsViaPhp(string phpDirectory, IEnumerable<string> extensions)
    {
        var exePath = Path.Combine(phpDirectory, "php.exe");
        var extList = extensions.ToList();
        
        if (!File.Exists(exePath) || extList.Count == 0)
        {
            return extList.ToDictionary(e => e, _ => "0");
        }

        var results = new Dictionary<string, string>();

        // Step 1: Use 'php -m' as a baseline. It's the most robust way to get loaded modules.
        try
        {
            var loaded = GetLoadedModules(phpDirectory);
            if (loaded.Count > 0)
            {
                foreach (var ext in extList)
                {
                    var normalizedExt = ext.ToLowerInvariant();
                    bool isLoaded = loaded.Any(m => {
                        var normalizedM = m.ToLowerInvariant();
                        if (normalizedM == normalizedExt) return true;
                        // Handle common aliases/differences
                        if (normalizedExt == "opcache" && normalizedM.Contains("opcache")) return true;
                        if (normalizedExt == "xdebug" && normalizedM.Contains("xdebug")) return true;
                        return false;
                    });
                    results[ext] = isLoaded ? "1" : "0";
                }
            }
        }
        catch { /* Fallback to script */ }

        // Step 2: Use the script to get more accurate data and 'extension_dir'.
        try
        {
            var extListForScript = extList.Select(e => e.Replace("'", "\\'")).ToList();
            var extString = string.Join(",", extListForScript);
            var script = "$a=explode(',', $argv[1]); $r=[]; foreach($a as $e) $r[$e]=extension_loaded($e)?'1':'0'; $r['__ext_dir']=ini_get('extension_dir'); echo 'JSON:'.json_encode($r);";
            
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                // We don't use -d display_errors=off here because we want to see errors if it fails,
                // and our JSON parsing logic is robust enough to handle the extra output.
                Arguments = $"-r \"{script}\" -- \"{extString}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Environment = { ["PHPRC"] = phpDirectory },
                WorkingDirectory = phpDirectory
            });

            if (process == null) return results;

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            
            if (!process.WaitForExit(5000))
            {
                process.Kill();
                if (results.Count == 0) results["__error"] = "PHP check timed out after 5 seconds.";
                return results;
            }

            var output = outputTask.Result;
            var error = errorTask.Result;

            // Try to find the JSON part in the output.
            var searchIn = output;
            var jsonPrefix = "JSON:";
            var index = output.IndexOf(jsonPrefix);
            if (index != -1)
            {
                searchIn = output.Substring(index + jsonPrefix.Length);
            }

            var firstBrace = searchIn.IndexOf('{');
            var lastBrace = searchIn.LastIndexOf('}');
            if (firstBrace != -1 && lastBrace != -1 && lastBrace > firstBrace)
            {
                var json = searchIn.Substring(firstBrace, lastBrace - firstBrace + 1);
                var scriptResults = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (scriptResults != null)
                {
                    // Merge/Override with script results as they are more accurate
                    foreach (var entry in scriptResults)
                    {
                        results[entry.Key] = entry.Value;
                    }
                    return results;
                }
            }
            
            if (results.Count == 0 && !string.IsNullOrWhiteSpace(error))
            {
                results["__error"] = error.Trim();
            }
        }
        catch (Exception ex)
        {
            if (results.Count == 0) results["__error"] = ex.Message;
        }

        return results;
    }
}