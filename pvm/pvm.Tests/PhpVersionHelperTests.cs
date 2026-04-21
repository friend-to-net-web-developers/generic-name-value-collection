using pvm.Helper;
using Xunit;

namespace pvm.Tests;

public class PhpVersionHelperTests
{
    [Theory]
    [InlineData("8.4", "84")]
    [InlineData("8.2", "82")]
    [InlineData("7.4.3", "743")]
    [InlineData("8.5.1", "851")]
    public void ToSlug_RemovesDots(string version, string expected)
    {
        Assert.Equal(expected, PhpVersionHelper.ToSlug(version));
    }

    // Note: ToDotted inserts a dot after the first character only.
    // 2-char slugs ("84") -> "8.4"; 3-char slugs ("743") -> "7.43" (not "7.4.3").
    // In practice slugs are always 2 digits (major+minor), matching GetInstalledSlugs output.
    [Theory]
    [InlineData("84", "8.4")]
    [InlineData("82", "8.2")]
    [InlineData("85", "8.5")]
    [InlineData("74", "7.4")]
    public void ToDotted_TwoCharSlug_InsertsDot(string slug, string expected)
    {
        Assert.Equal(expected, PhpVersionHelper.ToDotted(slug));
    }

    [Theory]
    [InlineData("743", "7.43")]
    [InlineData("851", "8.51")]
    public void ToDotted_ThreeCharSlug_InsertsDotAfterFirst(string slug, string expected)
    {
        Assert.Equal(expected, PhpVersionHelper.ToDotted(slug));
    }

    [Fact]
    public void GetPhpRoot_ReturnsEnvVarWhenSet()
    {
        Environment.SetEnvironmentVariable("PVM_PHP_ROOT", @"D:\custom-php");
        try
        {
            Assert.Equal(@"D:\custom-php", PhpVersionHelper.GetPhpRoot());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PVM_PHP_ROOT", null);
        }
    }

    [Fact]
    public void GetPhpRoot_ReturnsDefaultWhenEnvVarNotSet()
    {
        var prev = Environment.GetEnvironmentVariable("PVM_PHP_ROOT");
        Environment.SetEnvironmentVariable("PVM_PHP_ROOT", null);
        try
        {
            Assert.Equal(@"C:\php", PhpVersionHelper.GetPhpRoot());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PVM_PHP_ROOT", prev);
        }
    }

    [Fact]
    public void GetInstalledSlugs_ReturnsEmpty_WhenRootDoesNotExist()
    {
        Environment.SetEnvironmentVariable("PVM_PHP_ROOT", @"C:\this-path-should-not-exist-pvm-test");
        try
        {
            var slugs = PhpVersionHelper.GetInstalledSlugs();
            Assert.Empty(slugs);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PVM_PHP_ROOT", null);
        }
    }

    [Fact]
    public void GetDetectedSlug_ReturnsActiveSlugWhenJunctionExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "pvm_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var php85 = Path.Combine(tempDir, "php85");
            Directory.CreateDirectory(php85);
            
            // Create a symbolic link/junction using JunctionHelper
            JunctionHelper.SetActive(tempDir, php85);

            Environment.SetEnvironmentVariable("PVM_PHP_ROOT", tempDir);
            var info = PhpVersionHelper.GetDetectedVersionInfo();
            Assert.NotNull(info);
            Assert.Equal("85", info.Slug);
            Assert.Equal(PhpVersionHelper.DetectionSource.ActiveJunction, info.Source);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PVM_PHP_ROOT", null);
            if (Directory.Exists(tempDir))
            {
                var active = Path.Combine(tempDir, "active");
                if (Directory.Exists(active)) Directory.Delete(active);
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void GetDetectedSlug_ReturnsSlugFromPath_WhenNoJunctionExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "pvm_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var php84 = Path.Combine(tempDir, "php84");
            Directory.CreateDirectory(php84);
            File.WriteAllText(Path.Combine(php84, "php.exe"), "");

            Environment.SetEnvironmentVariable("PVM_PHP_ROOT", tempDir);
            
            var oldPath = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", php84 + ";" + oldPath);
            try 
            {
                var info = PhpVersionHelper.GetDetectedVersionInfo();
                Assert.NotNull(info);
                Assert.Equal("84", info.Slug);
                Assert.Equal(PhpVersionHelper.DetectionSource.Path, info.Source);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", oldPath);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("PVM_PHP_ROOT", null);
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
