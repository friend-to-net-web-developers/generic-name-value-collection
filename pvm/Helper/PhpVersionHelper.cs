using System.Diagnostics;
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
                UseShellExecute = false
            });
            var output = result?.StandardOutput.ReadLine();
            var match = SemanticVersionRegex().Match(output ?? "");
            return match.Success ? match.Groups[1].Value : null;
        }
        catch { return null; }
    }

    [GeneratedRegex(@"PHP (\d+\.\d+\.\d+)")]
    private static partial Regex SemanticVersionRegex();
}