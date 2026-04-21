using System.Security.Principal;

namespace pvm.Helper;

public static class AdminHelper
{
    public static bool IsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void AssertAdmin()
    {
        if (!IsAdmin())
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: This command requires elevated (admin) privileges.");
            Console.WriteLine("Re-run PowerShell as Administrator and try again.");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }
}
