using pvm.Helper;
using Xunit;

namespace pvm.Tests;

public class JunctionHelperTests : IDisposable
{
    private readonly string _tempDir;

    public JunctionHelperTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "pvm_junction_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try
            {
                var activeLink = Path.Combine(_tempDir, "active");
                if (Directory.Exists(activeLink))
                {
                    Directory.Delete(activeLink);
                }
            }
            catch { }
            Directory.Delete(_tempDir, true);
        }
    }

    [Fact]
    public void SetActive_CreatesJunction()
    {
        var targetDir = Path.Combine(_tempDir, "php84");
        Directory.CreateDirectory(targetDir);
        File.WriteAllText(Path.Combine(targetDir, "test.txt"), "hello");

        JunctionHelper.SetActive(_tempDir, targetDir);

        var activeLink = Path.Combine(_tempDir, "active");
        Assert.True(Directory.Exists(activeLink));
        
        var testFile = Path.Combine(activeLink, "test.txt");
        Assert.True(File.Exists(testFile));
        Assert.Equal("hello", File.ReadAllText(testFile));
        
        // Check if it's a junction (reparse point)
        var di = new DirectoryInfo(activeLink);
        Assert.True((di.Attributes & FileAttributes.ReparsePoint) != 0);
    }

    [Fact]
    public void SetActive_UpdatesExistingJunction()
    {
        var target1 = Path.Combine(_tempDir, "php84");
        var target2 = Path.Combine(_tempDir, "php85");
        Directory.CreateDirectory(target1);
        Directory.CreateDirectory(target2);
        File.WriteAllText(Path.Combine(target1, "ver.txt"), "84");
        File.WriteAllText(Path.Combine(target2, "ver.txt"), "85");

        JunctionHelper.SetActive(_tempDir, target1);
        Assert.Equal("84", File.ReadAllText(Path.Combine(_tempDir, "active", "ver.txt")));

        JunctionHelper.SetActive(_tempDir, target2);
        Assert.Equal("85", File.ReadAllText(Path.Combine(_tempDir, "active", "ver.txt")));
    }
}
