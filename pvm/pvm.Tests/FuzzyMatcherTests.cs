using pvm.Helper;
using Xunit;

namespace pvm.Tests;

public class FuzzyMatcherTests
{
    [Theory]
    [InlineData("opensl", new[] { "openssl", "curl", "mbstring" }, "openssl")]
    [InlineData("cur", new[] { "openssl", "curl", "mbstring" }, "curl")]
    [InlineData("mbstr", new[] { "openssl", "curl", "mbstring" }, "mbstring")]
    [InlineData("xyz", new[] { "openssl", "curl", "mbstring" }, null)]
    public void Suggest_ReturnsExpectedMatch(string input, string[] candidates, string expected)
    {
        var result = FuzzyMatcher.Suggest(input, candidates);
        Assert.Equal(expected, result);
    }
}
