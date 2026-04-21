using System.CommandLine;
using pvm.Commands;

var rootCommand = new RootCommand("pvm - PHP Version Manager")
{
    new ListCommand(),
    new UseCommand(),
    new InstallCommand(),
    new UpdateCommand(),
    new EnableCommand(),
    new DisableCommand(),
    new CurrentCommand()
};

return await rootCommand.Parse(args).InvokeAsync();