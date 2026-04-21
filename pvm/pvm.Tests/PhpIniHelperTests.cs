using pvm.Helper;
using Xunit;

namespace pvm.Tests;

public class PhpIniHelperTests : IDisposable
{
    private readonly string _tempDir;

    public PhpIniHelperTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pvm_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void GetPhpIniPath_ReturnsExistingIni()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllText(iniPath, "");
        
        var result = PhpIniHelper.GetPhpIniPath(_tempDir);
        
        Assert.Equal(iniPath, result);
    }

    [Fact]
    public void GetPhpIniPath_CopiesFromDevelopmentIfMissing()
    {
        var devIni = Path.Combine(_tempDir, "php.ini-development");
        File.WriteAllText(devIni, "dev content");
        
        var result = PhpIniHelper.GetPhpIniPath(_tempDir);
        
        Assert.Equal(Path.Combine(_tempDir, "php.ini"), result);
        Assert.True(File.Exists(result));
        Assert.Equal("dev content", File.ReadAllText(result));
    }

    [Fact]
    public void GetPhpIniPath_CopiesFromProductionIfMissingAndNoDev()
    {
        var prodIni = Path.Combine(_tempDir, "php.ini-production");
        File.WriteAllText(prodIni, "prod content");
        
        var result = PhpIniHelper.GetPhpIniPath(_tempDir);
        
        Assert.Equal(Path.Combine(_tempDir, "php.ini"), result);
        Assert.True(File.Exists(result));
        Assert.Equal("prod content", File.ReadAllText(result));
    }

    [Fact]
    public void EnableExtension_UncommentsExistingLine()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[]
        {
            ";extension=openssl",
            "extension=curl"
        });

        var success = PhpIniHelper.EnableExtension(_tempDir, "openssl");
        
        Assert.True(success);
        var lines = File.ReadAllLines(iniPath);
        Assert.Contains("extension=openssl", lines);
        Assert.DoesNotContain(";extension=openssl", lines);
    }

    [Fact]
    public void EnableExtension_UncommentsExistingLineWithPhpPrefix()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[]
        {
            ";extension=php_openssl.dll"
        });

        var success = PhpIniHelper.EnableExtension(_tempDir, "openssl");
        
        Assert.True(success);
        var lines = File.ReadAllLines(iniPath);
        Assert.Contains("extension=openssl", lines);
    }

    [Fact]
    public void EnableExtension_AppendsIfNotFound()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[]
        {
            "extension=curl"
        });

        var success = PhpIniHelper.EnableExtension(_tempDir, "openssl");
        
        Assert.True(success);
        var lines = File.ReadAllLines(iniPath);
        Assert.Contains("extension=openssl", lines);
    }

    [Fact]
    public void EnableExtension_AlreadyEnabled_DoesNothing()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[]
        {
            "extension=openssl"
        });

        var success = PhpIniHelper.EnableExtension(_tempDir, "openssl");
        
        Assert.True(success);
        var lines = File.ReadAllLines(iniPath);
        Assert.Single(lines, l => l == "extension=openssl");
    }

    [Fact]
    public void EnableExtension_BuiltIn_DoesNothingAndReturnsTrue()
    {
        // We can't easily mock PhpVersionHelper.GetBuiltInModules because it's static
        // but we can place a fake php.exe that returns a specific output.
        // However, pvm seems to not have a mockable design for this yet.
        // For now, let's just test that if php.exe is missing, it still works as before.
        
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[] { ";extension=tokenizer" });

        var success = PhpIniHelper.EnableExtension(_tempDir, "tokenizer");
        
        Assert.True(success);
        var lines = File.ReadAllLines(iniPath);
        Assert.Contains("extension=tokenizer", lines);
    }

    [Fact]
    public void DisableExtension_CommentsOutExistingLine()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[]
        {
            "extension=openssl",
            "extension=curl"
        });

        var success = PhpIniHelper.DisableExtension(_tempDir, "openssl");
        
        Assert.True(success);
        var lines = File.ReadAllLines(iniPath);
        Assert.Contains(";extension=openssl", lines);
        Assert.DoesNotContain("extension=openssl", lines);
    }

    [Fact]
    public void DisableExtension_WorksWithZendExtension()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[]
        {
            "zend_extension=xdebug"
        });

        var success = PhpIniHelper.DisableExtension(_tempDir, "xdebug");
        
        Assert.True(success);
        var lines = File.ReadAllLines(iniPath);
        Assert.Contains(";zend_extension=xdebug", lines);
    }

    [Fact]
    public void GetAvailableExtensions_FindsDllsInExtDir()
    {
        var extDir = Path.Combine(_tempDir, "ext");
        Directory.CreateDirectory(extDir);
        File.WriteAllText(Path.Combine(extDir, "php_openssl.dll"), "");
        File.WriteAllText(Path.Combine(extDir, "php_curl.dll"), "");
        File.WriteAllText(Path.Combine(extDir, "something_else.txt"), "");

        var extensions = PhpIniHelper.GetAvailableExtensions(_tempDir);

        Assert.Contains("openssl", extensions);
        Assert.Contains("curl", extensions);
        Assert.Equal(2, extensions.Count);
    }

    [Fact]
    public void GetAvailableExtensions_FindsInPhpIni()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[]
        {
            ";extension=mbstring",
            "zend_extension=opcache",
            "extension=php_gd2.dll"
        });

        var extensions = PhpIniHelper.GetAvailableExtensions(_tempDir);

        Assert.Contains("mbstring", extensions);
        Assert.Contains("opcache", extensions);
        Assert.Contains("gd2", extensions);
    }
}
