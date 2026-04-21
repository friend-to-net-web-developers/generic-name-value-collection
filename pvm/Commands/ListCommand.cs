using System.CommandLine;
using System.CommandLine.Invocation;
using pvm.Helper;

namespace pvm.Commands;

public class ListCommand : Command
{
    public ListCommand() : base("list", "List installed versions")
    {
        var presetsOption = new Option<bool>("--presets") { Description = "List available framework presets" };
        Add(presetsOption);

        this.SetAction(parseResult =>
        {
            var showPresets = parseResult.GetValue(presetsOption);
            if (showPresets)
            {
                Console.WriteLine("Available framework presets (for use with 'pvm enable'):");
                foreach (var preset in PhpIniHelper.ExtensionPresets)
                {
                    Console.WriteLine($"  --{preset.Key.ToLower().PadRight(10)} : {string.Join(", ", preset.Value)}");
                }
                return;
            }

            var phpRoot = PhpVersionHelper.GetPhpRoot();
            var activeSlug = PhpVersionHelper.GetCurrentActiveSlug();
            var installedSlugs = PhpVersionHelper.GetInstalledSlugs();

            foreach (var slug in installedSlugs)
            {
                var dotted = PhpVersionHelper.ToDotted(slug);
                var marker = slug == activeSlug ? " <-- active" : "";
                var exePath = Path.Combine(phpRoot, "php" + slug, "php.exe");
                var reported = PhpVersionHelper.GetVersionFromExe(exePath);
                var verLabel = reported != null ? $" ({reported})" : " (php.exe not found)";
                Console.WriteLine($"  {dotted}{verLabel}{marker}");
            }
        });
    }
}
