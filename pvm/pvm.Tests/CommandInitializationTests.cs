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

    [Theory]
    [InlineData("laravel")]
    [InlineData("wordpress")]
    [InlineData("drupal")]
    [InlineData("joomla")]
    [InlineData("magento")]
    [InlineData("twig")]
    [InlineData("composer")]
    [InlineData("symfony")]
    [InlineData("codeigniter")]
    [InlineData("cakephp")]
    [InlineData("slim")]
    public void EnableCommand_HasPresetOptions(string preset)
    {
        var command = new EnableCommand();
        Assert.Contains(command.Options, o => o.Name == "--" + preset);
    }

    [Fact]
    public void ListCommand_HasPresetsOption()
    {
        var command = new ListCommand();
        Assert.Contains(command.Options, o => o.Name == "--presets");
    }

    [Fact]
    public void DisableCommand_CanBeInstantiated()
    {
        var command = new DisableCommand();
        Assert.NotNull(command);
    }

    [Fact]
    public void CheckCommand_CanBeInstantiated()
    {
        var command = new CheckCommand();
        Assert.NotNull(command);
    }

    [Theory]
    [InlineData("laravel")]
    [InlineData("wordpress")]
    [InlineData("drupal")]
    public void CheckCommand_HasPresetOptions(string preset)
    {
        var command = new CheckCommand();
        Assert.Contains(command.Options, o => o.Name == "--" + preset);
    }
}
