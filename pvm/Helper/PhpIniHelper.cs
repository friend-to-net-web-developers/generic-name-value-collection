using System.Text.RegularExpressions;

namespace pvm.Helper;

public static class PhpIniHelper
{
    public static readonly Dictionary<string, string[]> ExtensionPresets = new(StringComparer.OrdinalIgnoreCase)
    {
        { "laravel", new[] { "bcmath", "ctype", "curl", "dom", "fileinfo", "mbstring", "openssl", "pdo", "pdo_mysql", "tokenizer", "xml", "zip" } },
        { "wordpress", new[] { "curl", "dom", "exif", "fileinfo", "gd", "imagick", "intl", "mbstring", "mysqli", "openssl", "pdo_mysql", "xml", "zip" } },
        { "drupal", new[] { "curl", "dom", "fileinfo", "gd", "intl", "mbstring", "openssl", "pdo", "pdo_mysql", "tokenizer", "xml", "zip" } },
        { "joomla", new[] { "curl", "dom", "gd", "intl", "mbstring", "mysqli", "openssl", "xml", "zip" } },
        { "magento", new[] { "bcmath", "ctype", "curl", "dom", "fileinfo", "gd", "intl", "mbstring", "mysqli", "openssl", "pdo_mysql", "soap", "sockets", "sodium", "xsl", "zip" } },
        { "twig", new[] { "ctype", "iconv", "mbstring" } },
        { "composer", new[] { "curl", "openssl", "zip", "zlib" } },
        { "symfony", new[] { "curl", "ctype", "iconv", "intl", "mbstring", "openssl", "pdo", "pdo_mysql", "tokenizer", "xml", "zip" } },
        { "codeigniter", new[] { "curl", "intl", "mbstring", "mysqli", "xml" } },
        { "cakephp", new[] { "intl", "mbstring", "pdo", "pdo_mysql", "simplexml" } },
        { "slim", new[] { "mbstring" } }
    };

    public static string[] DefaultExtensions => ExtensionPresets["laravel"];

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
        // Ensure extension_dir is set to "ext" so PHP can find the DLLs
        EnsureExtensionDir(phpDirectory);

        // If it's already a built-in module, we don't need to enable it in php.ini
        var builtIn = PhpVersionHelper.GetBuiltInModules(phpDirectory);
        if (builtIn.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            // If it's built-in, we should actually ensure it's NOT explicitly enabled in php.ini
            // as an external extension, to avoid "Unable to load dynamic library" warnings
            // especially on newer PHP versions where modules became built-in.
            DisableExtension(phpDirectory, extension);
            return true;
        }

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
        var pattern = $@"^;?\s*(?<type>zend_extension|extension)\s*=\s*(php_)?{Regex.Escape(extension)}(?:\.dll)?(?:\s*;.*)?\s*$";
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
        
        var pattern = $@"^\s*(?<type>zend_extension|extension)\s*=\s*(php_)?{Regex.Escape(extension)}(?:\.dll)?(?:\s*;.*)?\s*$";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        for (int i = 0; i < lines.Count; i++)
        {
            var match = regex.Match(lines[i]);
            if (match.Success)
            {
                var type = match.Groups["type"].Value;
                lines[i] = $";{type}={extension}";
                found = true;
            }
        }

        if (found)
        {
            File.WriteAllLines(iniPath, lines);
        }
        
        return true;
    }

    public static List<string> CleanupExtensions(string phpDirectory)
    {
        var disabled = new List<string>();
        var iniPath = GetPhpIniPath(phpDirectory);
        if (iniPath == null) return disabled;

        var builtIn = new HashSet<string>(PhpVersionHelper.GetBuiltInModules(phpDirectory), StringComparer.OrdinalIgnoreCase);

        var availableDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var extDir = Path.Combine(phpDirectory, "ext");
        if (Directory.Exists(extDir))
        {
            foreach (var file in Directory.GetFiles(extDir, "*.dll"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (name.StartsWith("php_", StringComparison.OrdinalIgnoreCase)) name = name[4..];
                availableDlls.Add(name);
            }
        }

        var lines = File.ReadAllLines(iniPath).ToList();
        var changed = false;

        // Match enabled extensions
        var pattern = @"^\s*(?<type>zend_extension|extension)\s*=\s*(?:php_)?(?<name>[\w\-]+)(?:\.dll)?(?:\s*;.*)?\s*$";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);

        for (int i = 0; i < lines.Count; i++)
        {
            var match = regex.Match(lines[i]);
            if (match.Success)
            {
                var name = match.Groups["name"].Value;
                var type = match.Groups["type"].Value;

                bool isBuiltIn = builtIn.Contains(name);
                bool dllExists = availableDlls.Contains(name);

                if (isBuiltIn || !dllExists)
                {
                    var reason = isBuiltIn ? "built-in" : "DLL missing";
                    lines[i] = $";{type}={name} ; Disabled by pvm ({reason})";
                    disabled.Add(name);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            File.WriteAllLines(iniPath, lines);
        }

        return disabled.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static List<string> GetAvailableExtensions(string phpDirectory)
    {
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Include built-in modules
        foreach (var mod in PhpVersionHelper.GetBuiltInModules(phpDirectory))
        {
            extensions.Add(mod);
        }

        // Check the 'ext' directory for .dll files (typical on Windows)
        var extDir = Path.Combine(phpDirectory, "ext");
        if (Directory.Exists(extDir))
        {
            foreach (var file in Directory.GetFiles(extDir, "*.dll"))
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
        var iniPath = GetPhpIniPath(phpDirectory);
        if (iniPath != null)
        {
            var lines = File.ReadAllLines(iniPath);
            var pattern = @"^;?\s*(?:zend_extension|extension)\s*=\s*(?:php_)?(?<name>[\w\-]+)(?:\.dll)?(?:\s*;.*)?\s*$";
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

    public static bool EnsureExtensionDir(string phpDirectory)
    {
        var iniPath = GetPhpIniPath(phpDirectory);
        if (iniPath == null) return false;

        var lines = File.ReadAllLines(iniPath).ToList();
        var changed = false;
        
        // Match extension_dir = "ext" or extension_dir = ext
        // Also match commented out ones
        var pattern = @"^;?\s*extension_dir\s*=\s*""?(?<dir>[^""]*)""?\s*(?:;.*)?$";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        
        bool found = false;
        for (int i = 0; i < lines.Count; i++)
        {
            var match = regex.Match(lines[i]);
            if (match.Success)
            {
                found = true;
                var currentDir = match.Groups["dir"].Value.Trim();
                
                // If it's commented out or set to something else, set it to "ext"
                if (lines[i].TrimStart().StartsWith(";") || (currentDir != "ext" && currentDir != "./ext" && !currentDir.EndsWith("\\ext")))
                {
                    lines[i] = "extension_dir = \"ext\"";
                    changed = true;
                }
                break;
            }
        }
        
        if (!found)
        {
            // Add it if not found at all
            // Try to find a good place to add it, e.g. after [PHP] tag or at the top
            var phpTagIndex = lines.FindIndex(l => l.Trim().Equals("[PHP]", StringComparison.OrdinalIgnoreCase));
            if (phpTagIndex != -1)
            {
                lines.Insert(phpTagIndex + 1, "extension_dir = \"ext\"");
            }
            else
            {
                lines.Insert(0, "extension_dir = \"ext\"");
            }
            changed = true;
        }
        
        if (changed)
        {
            File.WriteAllLines(iniPath, lines);
        }
        return true;
    }
}