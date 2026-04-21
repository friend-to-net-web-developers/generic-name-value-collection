using System.CommandLine;
using pvm.Helper;

namespace pvm.Commands;

public class InstallCommand : Command
{
    public InstallCommand(string name = "install", string description = "Install via Chocolatey") : base(name, description)
    {
        var versionArgument = new Argument<string>("version") { Description = "The PHP version to install (e.g., 8.5 or 8.5.4)" };
        Add(versionArgument);

        this.SetAction(parseResult =>
        {
            AdminHelper.AssertAdmin();

            if (!ChocoHelper.IsChocoInstalled())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Chocolatey is not installed or not on PATH.");
                Console.WriteLine("  Install it from https://chocolatey.org/install");
                Console.ResetColor();
                Environment.Exit(1);
            }

            var version = parseResult.GetValue(versionArgument);
            if (string.IsNullOrEmpty(version)) return;

            var fullVersion = ChocoHelper.ResolveFullVersion(version);
            var slug = PhpVersionHelper.ToSlug(fullVersion.Split('.')[0] + "." + fullVersion.Split('.')[1]);
            var phpRoot = PhpVersionHelper.GetPhpRoot();
            var installDir = Path.Combine(phpRoot, "php" + slug);
            var activeLink = Path.Combine(phpRoot, "active");
            var currentActiveSlug = PhpVersionHelper.GetCurrentActiveSlug();
            var stashRoot = Path.Combine(Path.GetTempPath(), "pvm_stash");

            // Break junction
            if (Directory.Exists(activeLink))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Temporarily removing active junction...");
                Console.ResetColor();
                Directory.Delete(activeLink);
            }

            if (!Directory.Exists(stashRoot)) Directory.CreateDirectory(stashRoot);

            // Stash other versions
            var stashed = new List<(string Original, string Stash, string Name)>();
            foreach (var dir in Directory.GetDirectories(phpRoot, "php*"))
            {
                var name = Path.GetFileName(dir);
                if (name != "php" + slug)
                {
                    var stashDest = Path.Combine(stashRoot, name);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Stashing {name} -> {stashDest} ...");
                    Console.ResetColor();
                    if (Directory.Exists(stashDest)) Directory.Delete(stashDest, true);
                    Directory.Move(dir, stashDest);
                    stashed.Add((dir, stashDest, name));
                }
            }

            // Clean target
            if (Directory.Exists(installDir))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Removing existing {installDir} ...");
                Console.ResetColor();
                Directory.Delete(installDir, true);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Installing PHP {fullVersion} into {installDir} ...");
            Console.ResetColor();

            int exitCode = ChocoHelper.Install(fullVersion, installDir);
            bool success = exitCode == 0;

            // Restore stashed
            foreach (var s in stashed)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Restoring {s.Name} ...");
                Console.ResetColor();
                if (Directory.Exists(s.Original)) Directory.Delete(s.Original, true);
                Directory.Move(s.Stash, s.Original);
            }

            if (Directory.Exists(stashRoot)) Directory.Delete(stashRoot, true);

            // Restore junction
            if (currentActiveSlug != null)
            {
                var target = Path.Combine(phpRoot, "php" + currentActiveSlug);
                if (Directory.Exists(target))
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"Restoring active junction to php{currentActiveSlug} ...");
                    Console.ResetColor();
                    JunctionHelper.SetActive(phpRoot, target);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"WARNING: Previous active 'php{currentActiveSlug}' no longer exists, junction not restored.");
                    Console.WriteLine("  Run 'pvm use <version>' to set a new active version.");
                    Console.ResetColor();
                }
            }

            if (success)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"PHP {fullVersion} installed successfully at {installDir}");
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Enabling default extensions for Laravel/Composer...");
                Console.ResetColor();

                foreach (var extension in PhpIniHelper.DefaultExtensions)
                {
                    if (PhpIniHelper.EnableExtension(installDir, extension))
                    {
                        Console.WriteLine($"  Enabled {extension}");
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Run 'pvm use {PhpVersionHelper.ToDotted(slug)}' to activate it.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Chocolatey reported an error. Check output above.");
                Console.ResetColor();
            }
        });
    }
}
