using System.CommandLine;
using pvm.Helper;
using Spectre.Console;

namespace pvm.Commands;

public class CheckCommand : Command
{
    public CheckCommand() : base("check", "Check if PHP extensions are enabled")
    {
        var extensionsArgument = new Argument<string[]>("extensions")
        {
            Arity = ArgumentArity.ZeroOrMore,
            Description = "The name(s) of the extension(s) to check"
        };
        Add(extensionsArgument);

        var allOption = new Option<bool>("--all") { Description = "Check for all installed versions" };
        Add(allOption);

        var versionOption = new Option<string>("--version") { Description = "Check for a specific version" };
        Add(versionOption);

        var presetOptions = new Dictionary<string, Option<bool>>(StringComparer.OrdinalIgnoreCase);
        foreach (var preset in PhpIniHelper.ExtensionPresets)
        {
            var displayName = char.ToUpper(preset.Key[0]) + preset.Key[1..];
            if (preset.Key.Equals("wordpress", StringComparison.OrdinalIgnoreCase)) displayName = "WordPress";
            if (preset.Key.Equals("codeigniter", StringComparison.OrdinalIgnoreCase)) displayName = "CodeIgniter";
            if (preset.Key.Equals("cakephp", StringComparison.OrdinalIgnoreCase)) displayName = "CakePHP";
            
            var description = $"Check if all extensions for {displayName} are enabled";
            
            var option = new Option<bool>($"--{preset.Key.ToLower()}") { Description = description };
            presetOptions[preset.Key] = option;
            Add(option);
        }

        this.SetAction(parseResult =>
        {
            var extensions = parseResult.GetValue(extensionsArgument) ?? Array.Empty<string>();
            var extensionList = extensions.ToList();
            bool anyPreset = false;
            foreach (var preset in presetOptions)
            {
                if (parseResult.GetValue(preset.Value))
                {
                    extensionList.AddRange(PhpIniHelper.ExtensionPresets[preset.Key]);
                    anyPreset = true;
                }
            }

            if (extensions.Length == 0 && !anyPreset)
            {
                if (!AnsiConsole.Profile.Capabilities.Interactive)
                {
                    AnsiConsole.MarkupLine("[red]Error: Specify at least one extension or use a framework preset (e.g., --laravel).[/]");
                    return 1;
                }

                var selected = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("Select [green]framework presets[/] to check:")
                        .NotRequired()
                        .PageSize(15)
                        .AddChoices(PhpIniHelper.ExtensionPresets.Keys.OrderBy(k => k)));

                if (selected.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No extensions or presets selected. Nothing to check.[/]");
                    return 0;
                }

                foreach (var p in selected)
                {
                    extensionList.AddRange(PhpIniHelper.ExtensionPresets[p]);
                }
                
                anyPreset = true;
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
            }
            else if (!string.IsNullOrEmpty(version))
            {
                var slug = PhpVersionHelper.ToSlug(version);
                var target = Path.Combine(phpRoot, "php" + slug);
                if (!Directory.Exists(target))
                {
                    AnsiConsole.MarkupLine($"[red]Error: PHP version {version} not found.[/]");
                    return 1;
                }
                targets.Add(target);
            }
            else
            {
                var detection = PhpVersionHelper.GetDetectedVersionInfo();
                if (detection == null)
                {
                    AnsiConsole.MarkupLine("[red]Error: No PHP version detected.[/]");
                    return 1;
                }
                targets.Add(Path.Combine(phpRoot, "php" + detection.Slug));
            }

            bool allSucceeded = true;
            foreach (var target in targets)
            {
                var slug = Path.GetFileName(target).Replace("php", "");
                var versionDisplay = PhpVersionHelper.ToDotted(slug);
                
                // Use the more accurate PHP-based check.
                // This verifies that the extension is not only configured but successfully initialized by the engine.
                var checkResults = PhpVersionHelper.CheckExtensionsViaPhp(target, extensionList);
                
                if (checkResults.Count == 0 && extensionList.Count > 0)
                {
                    AnsiConsole.MarkupLine($"[red][[ERROR]] Failed to run PHP check for version {versionDisplay}.[/]");
                    allSucceeded = false;
                    continue;
                }

                foreach (var entry in checkResults)
                {
                    var ext = entry.Key;
                    if (ext == "__ext_dir") continue;
                    
                    bool isLoaded = entry.Value == "1";
                    
                    if (isLoaded)
                    {
                        AnsiConsole.MarkupLine($"[green][[OK]] {ext} is enabled for PHP {versionDisplay}[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red][[FAIL]] {ext} is NOT enabled for PHP {versionDisplay}[/]");
                        allSucceeded = false;
                    }
                }

                if (!allSucceeded && checkResults.TryGetValue("__ext_dir", out var extDir))
                {
                    // If everything failed, check if extension_dir is suspiciously empty or default
                    if (string.IsNullOrEmpty(extDir) || extDir == "C:\\php" || extDir == ".")
                    {
                        AnsiConsole.MarkupLine($"[yellow]Tip: extension_dir is set to '{extDir}'. pvm has updated your php.ini to use 'extension_dir = \"ext\"', but if issues persist, verify that the 'ext' folder exists and contains the necessary DLLs.[/]");
                    }
                }
            }

            return allSucceeded ? 0 : 1;
        });
    }
}
