using System.CommandLine;
using System.CommandLine.Invocation;
using pvm.Helper;

namespace pvm.Commands;

public class ListCommand : Command
{
    public ListCommand() : base("list", "List installed versions")
    {
        this.SetAction(context =>
        {
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
