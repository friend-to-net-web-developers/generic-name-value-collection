using pvm.Helper;
using Xunit;

namespace pvm.Tests;

public class ChocoHelperTests
{
    // ResolveFullVersion passes through immediately for already-complete versions.
    // The partial-version path (e.g. "8.5") invokes choco and is not unit-testable here.

    [Theory]
    [InlineData("8.5.4")]
    [InlineData("7.4.3")]
    [InlineData("8.2.0")]
    public void ResolveFullVersion_ReturnsUnchanged_WhenAlreadyFullVersion(string version)
    {
        Assert.Equal(version, ChocoHelper.ResolveFullVersion(version));
    }
}
