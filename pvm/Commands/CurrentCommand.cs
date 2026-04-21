using System.CommandLine;
using System.CommandLine.Invocation;
using pvm.Helper;

namespace pvm.Commands;

public class CurrentCommand : Command
{
    public CurrentCommand() : base("current", "Show active version")
    {
        this.SetAction(context =>
        {
            var activeSlug = PhpVersionHelper.GetCurrentActiveSlug();
            if (activeSlug != null)
            {
                var dotted = PhpVersionHelper.ToDotted(activeSlug);
                var phpRoot = PhpVersionHelper.GetPhpRoot();
                var exePath = Path.Combine(phpRoot, "php" + activeSlug, "php.exe");
                var reported = PhpVersionHelper.GetVersionFromExe(exePath);
                var verLabel = reported != null ? $" ({reported})" : " (php.exe not found)";
                Console.WriteLine($"  {dotted}{verLabel}");
            }
            else
            {
                Console.WriteLine("No active version set");
            }
            return 0;
        });
    }
}
