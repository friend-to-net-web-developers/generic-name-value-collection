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
            var detection = PhpVersionHelper.GetDetectedVersionInfo();
            if (detection != null)
            {
                var dotted = PhpVersionHelper.ToDotted(detection.Slug);
                var phpRoot = PhpVersionHelper.GetPhpRoot();
                var exePath = Path.Combine(phpRoot, "php" + detection.Slug, "php.exe");
                var reported = PhpVersionHelper.GetVersionFromExe(exePath);
                var verLabel = reported != null ? $" ({reported})" : "";
                var sourceLabel = detection.Source == PhpVersionHelper.DetectionSource.ActiveJunction 
                    ? "[active]" 
                    : "[detected from PATH]";
                
                Console.WriteLine($"  {dotted}{verLabel} {sourceLabel}");
            }
            else
            {
                Console.WriteLine("No active version set");
            }
            return 0;
        });
    }
}
