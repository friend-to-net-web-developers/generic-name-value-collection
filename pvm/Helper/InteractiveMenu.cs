using Spectre.Console;
using pvm.Helper;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace pvm.Helper;

public static class InteractiveMenu
{
    public static async Task<int> RunAsync(RootCommand rootCommand)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(
            new FigletText("PVM")
                .Color(Color.Green));
        AnsiConsole.MarkupLine("[grey]PHP Version Manager for Windows[/]\n");

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to [green]do[/]?")
                    .PageSize(10)
                    .AddChoices(new[] {
                        "List installed versions",
                        "Switch active version",
                        "Show current version",
                        "Enable extension",
                        "Apply framework preset",
                        "Disable extension",
                        "Install new version",
                        "Update version",
                        "Exit"
                    }));

            if (choice == "Exit") break;

            await HandleChoiceAsync(choice, rootCommand);

            AnsiConsole.MarkupLine("\n[grey]Press any key to return to menu...[/]");
            Console.ReadKey(true);
            AnsiConsole.Clear();
            
            AnsiConsole.Write(
                new FigletText("PVM")
                    .Color(Color.Green));
            AnsiConsole.MarkupLine("[grey]PHP Version Manager for Windows[/]\n");
        }

        return 0;
    }

    private static async Task HandleChoiceAsync(string choice, RootCommand rootCommand)
    {
        switch (choice)
        {
            case "List installed versions":
                await rootCommand.Parse(["list"]).InvokeAsync();
                break;
            case "Switch active version":
                await SwitchVersion(rootCommand);
                break;
            case "Show current version":
                await rootCommand.Parse(["current"]).InvokeAsync();
                break;
            case "Enable extension":
                await EnableExtension(rootCommand);
                break;
            case "Apply framework preset":
                await ApplyPreset(rootCommand);
                break;
            case "Disable extension":
                await DisableExtension(rootCommand);
                break;
            case "Install new version":
                await InstallVersion(rootCommand);
                break;
            case "Update version":
                await UpdateVersion(rootCommand);
                break;
        }
    }

    private static async Task SwitchVersion(RootCommand rootCommand)
    {
        var slugs = PhpVersionHelper.GetInstalledSlugs();
        if (slugs.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No PHP versions installed.[/]");
            return;
        }

        var activeSlug = PhpVersionHelper.GetCurrentActiveSlug();
        var choices = slugs.Select(s => 
        {
            var dotted = PhpVersionHelper.ToDotted(s);
            return s == activeSlug ? $"{dotted} [grey](active)[/]" : dotted;
        }).ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]version[/] to use:")
                .AddChoices(choices));

        var version = selected.Split(' ')[0];
        await rootCommand.Parse(["use", version]).InvokeAsync();
    }

    private static async Task EnableExtension(RootCommand rootCommand)
    {
        var detection = PhpVersionHelper.GetDetectedVersionInfo();
        if (detection == null)
        {
            AnsiConsole.MarkupLine("[red]No active PHP version found. Please use 'pvm use <version>' or ensure PHP is on your PATH.[/]");
            return;
        }

        var sourceLabel = detection.Source == PhpVersionHelper.DetectionSource.ActiveJunction 
            ? "active junction" 
            : "PATH";
        
        AnsiConsole.MarkupLine($"[cyan]Notice: Using PHP {PhpVersionHelper.ToDotted(detection.Slug)} (detected from {sourceLabel}).[/]");

        var phpRoot = PhpVersionHelper.GetPhpRoot();
        var target = Path.Combine(phpRoot, "php" + detection.Slug);
        var available = PhpIniHelper.GetAvailableExtensions(target);

        if (available.Count == 0)
        {
            var ext = AnsiConsole.Ask<string>("No extensions discovered automatically. Enter [green]extension name[/] to enable:");
            await rootCommand.Parse(["enable", ext]).InvokeAsync();
            return;
        }

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select [green]extensions[/] to enable:")
                .NotRequired()
                .PageSize(15)
                .AddChoices(available));

        if (selected.Count > 0)
        {
            var args = new List<string> { "enable" };
            args.AddRange(selected);
            await rootCommand.Parse(args.ToArray()).InvokeAsync();
        }
    }

    private static async Task ApplyPreset(RootCommand rootCommand)
    {
        var detection = PhpVersionHelper.GetDetectedVersionInfo();
        if (detection == null)
        {
            AnsiConsole.MarkupLine("[red]No active PHP version found.[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]framework preset[/] to apply:")
                .AddChoices(PhpIniHelper.ExtensionPresets.Keys.Select(k => k.ToLower())));

        await rootCommand.Parse(["enable", "--" + selected]).InvokeAsync();
    }

    private static async Task DisableExtension(RootCommand rootCommand)
    {
        var detection = PhpVersionHelper.GetDetectedVersionInfo();
        if (detection == null)
        {
            AnsiConsole.MarkupLine("[red]No active PHP version found.[/]");
            return;
        }

        var sourceLabel = detection.Source == PhpVersionHelper.DetectionSource.ActiveJunction 
            ? "active junction" 
            : "PATH";
        
        AnsiConsole.MarkupLine($"[cyan]Notice: Using PHP {PhpVersionHelper.ToDotted(detection.Slug)} (detected from {sourceLabel}).[/]");

        var ext = AnsiConsole.Ask<string>("Enter [green]extension name[/] to disable:");
        await rootCommand.Parse(["disable", ext]).InvokeAsync();
    }

    private static async Task InstallVersion(RootCommand rootCommand)
    {
        if (!AdminHelper.IsAdmin())
        {
            AnsiConsole.MarkupLine("[red]ERROR: Installing PHP requires Administrator privileges.[/]");
            return;
        }

        var version = AnsiConsole.Ask<string>("Enter the PHP [green]version[/] to install (e.g. 8.4.3):");
        await rootCommand.Parse(["install", version]).InvokeAsync();
    }

    private static async Task UpdateVersion(RootCommand rootCommand)
    {
        if (!AdminHelper.IsAdmin())
        {
            AnsiConsole.MarkupLine("[red]ERROR: Updating PHP requires Administrator privileges.[/]");
            return;
        }

        var slugs = PhpVersionHelper.GetInstalledSlugs();
        if (slugs.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No PHP versions installed to update.[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select [green]version[/] to update/reinstall:")
                .AddChoices(slugs.Select(PhpVersionHelper.ToDotted)));

        await rootCommand.Parse(["update", selected]).InvokeAsync();
    }
}
