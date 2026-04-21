using System.Text.RegularExpressions;

namespace pvm.Helper;

public static class PhpIniHelper
{
    public static string? GetPhpIniPath(string phpDirectory)
    {
        var iniPath = Path.Combine(phpDirectory, "php.ini");
        if (File.Exists(iniPath)) return iniPath;

        // If php.ini doesn't exist, try to find development or production ones
        var devIni = Path.Combine(phpDirectory, "php.ini-development");
        if (File.Exists(devIni))
        {
            try
            {
                File.Copy(devIni, iniPath);
                return iniPath;
            }
            catch
            {
                return null;
            }
        }

        var prodIni = Path.Combine(phpDirectory, "php.ini-production");
        if (File.Exists(prodIni))
        {
            try
            {
                File.Copy(prodIni, iniPath);
                return iniPath;
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    public static bool EnableExtension(string phpDirectory, string extension)
    {
        var iniPath = GetPhpIniPath(phpDirectory);
        if (iniPath == null)
        {
            return false;
        }

        var lines = File.ReadAllLines(iniPath).ToList();
        var found = false;
        
        // Match: 
        // ;extension=openssl
        // extension=openssl
        // ;zend_extension=xdebug
        // Also allow spaces around = and ;
        var pattern = $@"^;?\s*(?<type>zend_extension|extension)\s*=\s*(php_)?{Regex.Escape(extension)}(\.dll)?\s*$";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        for (int i = 0; i < lines.Count; i++)
        {
            var match = regex.Match(lines[i]);
            if (match.Success)
            {
                // Preserve the original if it was already enabled
                if (!lines[i].TrimStart().StartsWith(";"))
                {
                    return true; 
                }
                
                var type = match.Groups["type"].Value;
                lines[i] = $"{type}={extension}";
                found = true;
                break;
            }
        }

        if (!found)
        {
            var type = (extension.Contains("xdebug", StringComparison.OrdinalIgnoreCase) || 
                        extension.Contains("opcache", StringComparison.OrdinalIgnoreCase)) 
                ? "zend_extension" : "extension";
            lines.Add($"{type}={extension}");
        }

        File.WriteAllLines(iniPath, lines);
        return true;
    }

    public static bool DisableExtension(string phpDirectory, string extension)
    {
        var iniPath = GetPhpIniPath(phpDirectory);
        if (iniPath == null)
        {
            return false;
        }

        var lines = File.ReadAllLines(iniPath).ToList();
        var found = false;
        
        var pattern = $@"^\s*(?<type>zend_extension|extension)\s*=\s*(php_)?{Regex.Escape(extension)}(\.dll)?\s*$";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        for (int i = 0; i < lines.Count; i++)
        {
            var match = regex.Match(lines[i]);
            if (match.Success)
            {
                var type = match.Groups["type"].Value;
                lines[i] = $";{type}={extension}";
                found = true;
                break;
            }
        }

        if (found)
        {
            File.WriteAllLines(iniPath, lines);
        }
        
        return true;
    }

    public static List<string> GetAvailableExtensions(string phpDirectory)
    {
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Check the 'ext' directory for .dll files (typical on Windows)
        var extDir = Path.Combine(phpDirectory, "ext");
        if (Directory.Exists(extDir))
        {
            foreach (var file in Directory.GetFiles(extDir, "php_*.dll"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.StartsWith("php_", StringComparison.OrdinalIgnoreCase))
                {
                    name = name[4..]; // remove 'php_'
                }
                extensions.Add(name);
            }
        }

        // Also check php.ini for any mentioned extensions
        var iniPath = Path.Combine(phpDirectory, "php.ini");
        if (!File.Exists(iniPath))
        {
            // Try templates
            var devIni = Path.Combine(phpDirectory, "php.ini-development");
            var prodIni = Path.Combine(phpDirectory, "php.ini-production");
            if (File.Exists(devIni)) iniPath = devIni;
            else if (File.Exists(prodIni)) iniPath = prodIni;
            else iniPath = null;
        }

        if (iniPath != null)
        {
            var lines = File.ReadAllLines(iniPath);
            var pattern = @"^;?\s*(?:zend_extension|extension)\s*=\s*(?:php_)?(?<name>[\w\-]+)(?:\.dll)?\s*$";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            foreach (var line in lines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    extensions.Add(match.Groups["name"].Value);
                }
            }
        }

        return extensions.OrderBy(e => e).ToList();
    }
}