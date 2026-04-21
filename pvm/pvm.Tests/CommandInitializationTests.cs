using pvm.Commands;
using Xunit;

namespace pvm.Tests;

public class CommandInitializationTests
{
    [Fact]
    public void EnableCommand_CanBeInstantiated()
    {
        var command = new EnableCommand();
        Assert.NotNull(command);
    }

    [Fact]
    public void EnableCommand_HasLaravelOption()
    {
        var command = new EnableCommand();
        Assert.Contains(command.Options, o => o.Name == "laravel" || o.Name == "--laravel");
    }

    [Fact]
    public void DisableCommand_CanBeInstantiated()
    {
        var command = new DisableCommand();
        Assert.NotNull(command);
    }
}
