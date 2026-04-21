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
    public void DisableCommand_CanBeInstantiated()
    {
        var command = new DisableCommand();
        Assert.NotNull(command);
    }
}
