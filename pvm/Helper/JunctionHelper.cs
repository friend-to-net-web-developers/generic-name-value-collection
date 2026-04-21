using System.Diagnostics;

namespace pvm.Helper;

/// <summary>
/// Helper class for managing PHP junction links.
/// </summary>
public static class JunctionHelper
{
    /// <summary>
    /// Updates the active junction link within the specified PHP root directory to point to the given target directory.
    /// </summary>
    /// <param name="phpRoot">The root directory where the active junction link resides.</param>
    /// <param name="target">The target directory to which the active junction link should point.</param>
    public static void SetActive(string phpRoot, string target)
    {
        var activeLink = Path.Combine(phpRoot, "active");
        if (Directory.Exists(activeLink))
            Directory.Delete(activeLink); // deletes junction, not contents

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd",
            Arguments = $"/c mklink /j \"{activeLink}\" \"{target}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true
        });

        process?.WaitForExit();

        if (process?.ExitCode != 0)
        {
            var error = process?.StandardError.ReadToEnd() ?? "Unknown error";
            throw new UnauthorizedAccessException($"Failed to create junction link at {activeLink}. {error}");
        }
    }
}