using System.CommandLine;
using pvm.Commands;
using pvm.Helper;

var rootCommand = new RootCommand("pvm - PHP Version Manager")
{
    new ListCommand(),
    new UseCommand(),
    new InstallCommand(),
    new UpdateCommand(),
    new EnableCommand(),
    new DisableCommand(),
    new CheckCommand(),
    new CurrentCommand()
};

if (args.Length == 0)
{
    return await InteractiveMenu.RunAsync(rootCommand);
}

return await rootCommand.Parse(args).InvokeAsync();