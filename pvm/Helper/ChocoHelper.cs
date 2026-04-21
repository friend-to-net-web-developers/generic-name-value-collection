using System.Diagnostics;
using System.Text.RegularExpressions;

namespace pvm.Helper;

public static class ChocoHelper
{
    /// <summary>
    /// Checks whether Chocolatey is installed and accessible in the system's PATH.
    /// </summary>
    /// <returns>
    /// True if Chocolatey is installed and its executable can be invoked successfully; otherwise, false.
    /// </returns>
    public static bool IsChocoInstalled()
    {
        try
        {
            var result = Process.Start(new ProcessStartInfo
            {
                FileName = "choco",
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            result?.WaitForExit();
            return result?.ExitCode == 0;
        }
        catch { return false; }
    }

    /// <summary>
    /// Resolves the full version of a software by querying the system or Chocolatey,
    /// ensuring the version matches the expected format.
    /// </summary>
    /// <param name="version">
    /// The user-provided version string, which can be in major.minor or full format (e.g., 8.5 or 8.5.4).
    /// </param>
    /// <returns>
    /// The full version string in the format major.minor.patch if successful, or the original input if no resolution was needed.
    /// </returns>
    public static string ResolveFullVersion(string version)
    {
        if (Regex.IsMatch(version, @"^\d+\.\d+\.\d+$")) return version;
        if (!Regex.IsMatch(version, @"^\d+\.\d+$"))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Invalid version format '{version}'. Use major.minor (e.g. 8.5) or full (e.g. 8.5.4)");
            Console.ResetColor();
            Environment.Exit(1);
        }

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Querying Chocolatey for latest PHP {version}.x ...");
        Console.ResetColor();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "choco",
                Arguments = "search php --all-versions --exact",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            var process = Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd() ?? "";
            process?.WaitForExit();

            var versions = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("php "))
                .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1])
                .Where(v => v.StartsWith(version + "."))
                .Select(v => new Version(v))
                .OrderByDescending(v => v)
                .ToList();

            if (versions.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"No PHP {version}.x found in Chocolatey registry.");
                Console.ResetColor();
                Environment.Exit(1);
            }

            var resolved = versions[0].ToString();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Resolved {version} -> {resolved}");
            Console.ResetColor();
            return resolved;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error querying Chocolatey: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
            return null; // unreachable
        }
    }

    public static int Install(string fullVersion, string installDir)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "choco",
            Arguments = $"install php --force -y --version={fullVersion} --package-parameters=\"/InstallDir:{installDir} /DontAddToPath\" --ignore-dependencies",
            UseShellExecute = false
        };
        var process = Process.Start(startInfo);
        process?.WaitForExit();
        return process?.ExitCode ?? -1;
    }
}
