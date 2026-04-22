using System.CommandLine;
using pvm.Helper;

namespace pvm.Commands;

public class UseCommand : Command
{
    public UseCommand() : base("use", "Switch active version")
    {
        var versionArgument = new Argument<string>("version") { Description = "The PHP version to use (e.g., 8.4 or 84)" };
        Add(versionArgument);

        this.SetAction(parseResult =>
        {
            var version = parseResult.GetValue(versionArgument);
            if (string.IsNullOrEmpty(version)) return;

            var phpRoot = PhpVersionHelper.GetPhpRoot();
            var slug = PhpVersionHelper.ToSlug(version);
            var target = Path.Combine(phpRoot, "php" + slug);

            if (!Directory.Exists(target))
            {
                Console.WriteLine($"Version php{slug} not found in {phpRoot}");
                var available = PhpVersionHelper.GetInstalledSlugs().Select(PhpVersionHelper.ToDotted);
                Console.WriteLine($"Available: {string.Join(", ", available)}");
                Environment.Exit(1);
            }

            try
            {
                JunctionHelper.SetActive(phpRoot, target);
                
                // Ensure the configuration is sane
                PhpIniHelper.EnsureExtensionDir(target);
                var disabled = PhpIniHelper.CleanupExtensions(target);
                
                if (disabled.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Notice: Disabled redundant or missing extensions: {string.Join(", ", disabled)}");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Could not switch PHP version. {ex.Message}");
                if (ex is UnauthorizedAccessException)
                {
                    Console.WriteLine($"Make sure you have write permissions to {phpRoot} or run as Administrator.");
                }
                Console.ResetColor();
                Environment.Exit(1);
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Switched to PHP {PhpVersionHelper.ToDotted(slug)}  ({target})");
            Console.ResetColor();

            // Verify
            var activeExe = Path.Combine(phpRoot, "active", "php.exe");
            var reported = PhpVersionHelper.GetVersionFromExe(activeExe);
            if (reported != null)
            {
                var targetExe = Path.Combine(target, "php.exe");
                var folderVer = PhpVersionHelper.GetVersionFromExe(targetExe);
                if (folderVer != null)
                {
                    if (reported == folderVer)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"OK: php -v reports {reported}");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"WARNING: php -v reports {reported} but expected {folderVer}");
                        Console.WriteLine("         Something else on PATH may be intercepting 'php'.");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"OK: php -v reports {reported}");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARNING: Could not run php -v after switching.");
            }
            Console.ResetColor();

            // Scan PATH
            var allPhp = PhpVersionHelper.FindAllPhpOnPath();
            var activePath = Path.Combine(phpRoot, "active");
            var rogues = allPhp.Where(x => !x.Path.StartsWith(phpRoot, StringComparison.OrdinalIgnoreCase)).ToList();
            if (rogues.Count > 0)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("WARNING: Found other PHP installations on PATH that may cause conflicts:");
                foreach (var r in rogues)
                {
                    Console.WriteLine($"  {r.Path}  [version: {r.Version ?? "unknown"}]");
                }
                Console.WriteLine("  Consider removing these from PATH or uninstalling them.");
                Console.ResetColor();
            }
        });
    }
}