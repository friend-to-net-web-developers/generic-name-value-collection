using System.CommandLine;
using pvm.Helper;

namespace pvm.Commands;

public class EnableCommand : Command
{
    public EnableCommand() : base("enable", "Enable one or more PHP extensions")
    {
        var extensionsArgument = new Argument<string[]>("extensions")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description = "The name(s) of the extension(s) to enable (e.g., openssl curl)"
        };
        Add(extensionsArgument);

        var allOption = new Option<bool>("--all") { Description = "Enable for all installed versions" };
        Add(allOption);

        var versionOption = new Option<string>("--version") { Description = "Enable for a specific version" };
        Add(versionOption);

        var laravelOption = new Option<bool>("--laravel") { Description = "Enable all extensions necessary for Laravel/Composer" };
        Add(laravelOption);

        this.SetAction(parseResult =>
        {
            var extensions = parseResult.GetValue(extensionsArgument) ?? Array.Empty<string>();
            var laravel = parseResult.GetValue(laravelOption);

            if (extensions.Length == 0 && !laravel)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error: Specify at least one extension or use --laravel");
                Console.ResetColor();
                return;
            }

            var extensionList = extensions.ToList();
            if (laravel)
            {
                extensionList.AddRange(PhpIniHelper.DefaultExtensions);
            }

            extensionList = extensionList.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var all = parseResult.GetValue(allOption);
            var version = parseResult.GetValue(versionOption);

            var phpRoot = PhpVersionHelper.GetPhpRoot();
            var targets = new List<string>();

            if (all)
            {
                var slugs = PhpVersionHelper.GetInstalledSlugs();
                foreach (var slug in slugs)
                {
                    targets.Add(Path.Combine(phpRoot, "php" + slug));
                }
                
                if (targets.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No PHP versions found to enable extensions for.");
                    Console.ResetColor();
                    return;
                }
            }
            else if (!string.IsNullOrEmpty(version))
            {
                var slug = PhpVersionHelper.ToSlug(version);
                var target = Path.Combine(phpRoot, "php" + slug);
                if (!Directory.Exists(target))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: PHP version {version} (php{slug}) not found in {phpRoot}");
                    Console.ResetColor();
                    return;
                }
                targets.Add(target);
            }
            else
            {
                // Default to detected version (active junction or PATH)
                var detection = PhpVersionHelper.GetDetectedVersionInfo();
                if (detection == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: No active PHP version found. Use 'pvm use <version>' or specify --version or --all.");
                    Console.ResetColor();
                    return;
                }

                var sourceLabel = detection.Source == PhpVersionHelper.DetectionSource.ActiveJunction 
                    ? "active junction" 
                    : "PATH";
                
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"Notice: No version specified. Using PHP {PhpVersionHelper.ToDotted(detection.Slug)} (detected from {sourceLabel}).");
                Console.ResetColor();

                targets.Add(Path.Combine(phpRoot, "php" + detection.Slug));
            }

            foreach (var target in targets)
            {
                var slug = Path.GetFileName(target).Replace("php", "");
                var versionDisplay = PhpVersionHelper.ToDotted(slug);
                
                try 
                {
                    var availableExtensions = PhpIniHelper.GetAvailableExtensions(target);

                    foreach (var extension in extensionList)
                    {
                        if (availableExtensions.Count > 0 && !availableExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write($"Warning: Extension '{extension}' not found for PHP {versionDisplay}. ");
                            
                            var suggestion = FuzzyMatcher.Suggest(extension, availableExtensions);
                            if (suggestion != null)
                            {
                                Console.WriteLine($"Did you mean '{suggestion}'?");
                            }
                            else
                            {
                                Console.WriteLine();
                            }
                            Console.ResetColor();
                        }

                        if (PhpIniHelper.EnableExtension(target, extension))
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Successfully enabled '{extension}' for PHP {versionDisplay}");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Failed to enable '{extension}' for PHP {versionDisplay}. (php.ini not found and could not be created)");
                            Console.ResetColor();
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Permission denied when accessing PHP {versionDisplay} configuration.");
                    Console.WriteLine($"Make sure you have write permissions to {target} or run as Administrator.");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: Failed to enable extensions for PHP {versionDisplay}: {ex.Message}");
                    Console.ResetColor();
                }
            }
        });
    }
}
