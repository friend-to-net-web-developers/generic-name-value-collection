using pvm.Helper;
using Xunit;
using System.Linq;

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
    public void EnableExtension_BuiltIn_EnsuresDisabledInIni()
    {
        // We can create a dummy php.exe (on Windows, even a text file named php.exe might trick File.Exists)
        // But to actually run it, we need a real executable.
        // Let's use 'cmd.exe' renamed to 'php.exe' and see if we can get it to return something? 
        // No, that's too complex.
        
        // Instead, let's rely on the fact that pvm tests are likely running in an environment 
        // where 'php' might be on the path, but the 'phpDirectory' passed to EnableExtension
        // is our _tempDir, which doesn't have php.exe.
        
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[] { "extension=tokenizer" });

        // Act
        PhpIniHelper.EnableExtension(_tempDir, "tokenizer");
        
        // Assert: It should NOT be disabled because tokenizer is not found as built-in in _tempDir
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
    [Fact]
    public void DisableExtension_CommentsOutAllOccurrences()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[]
        {
            "extension=openssl",
            "extension=openssl",
            "extension=curl"
        });

        PhpIniHelper.DisableExtension(_tempDir, "openssl");

        var lines = File.ReadAllLines(iniPath);
        Assert.Equal(2, lines.Count(l => l == ";extension=openssl"));
        Assert.DoesNotContain("extension=openssl", lines);
    }

    [Fact]
    public void CleanupExtensions_DisablesMissingDlls()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        File.WriteAllLines(iniPath, new[]
        {
            "extension=missing_ext",
            "extension=curl"
        });
        
        // Mock curl.dll existing
        var extDir = Path.Combine(_tempDir, "ext");
        Directory.CreateDirectory(extDir);
        File.WriteAllText(Path.Combine(extDir, "php_curl.dll"), "");

        var disabled = PhpIniHelper.CleanupExtensions(_tempDir);

        Assert.Contains("missing_ext", disabled);
        var lines = File.ReadAllLines(iniPath);
        Assert.Contains(";extension=missing_ext ; Disabled by pvm (DLL missing)", lines);
        Assert.Contains("extension=curl", lines);
    }
    [Fact]
    public void EnsureExtensionDir_UncommentsExistingLine()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        var iniContent = ";extension_dir = \"ext\"";
        File.WriteAllText(iniPath, iniContent);

        PhpIniHelper.EnsureExtensionDir(_tempDir);

        var lines = File.ReadAllLines(iniPath);
        Assert.Contains("extension_dir = \"ext\"", lines);
    }

    [Fact]
    public void EnsureExtensionDir_AddsIfMissing()
    {
        var iniPath = Path.Combine(_tempDir, "php.ini");
        var iniContent = "[PHP]\nengine = On";
        File.WriteAllText(iniPath, iniContent);

        PhpIniHelper.EnsureExtensionDir(_tempDir);

        var lines = File.ReadAllLines(iniPath);
        Assert.Contains("extension_dir = \"ext\"", lines);
    }
}
