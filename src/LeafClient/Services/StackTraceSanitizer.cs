using System;
using System.Text.RegularExpressions;

namespace LeafClient.Services;

internal static partial class StackTraceSanitizer
{
    private const int MaxLength = 4000;

    [GeneratedRegex(@"[A-Za-z]:\\Users\\[^\\]+\\", RegexOptions.IgnoreCase)]
    private static partial Regex UserPathPattern();

    [GeneratedRegex(@"[A-Za-z]:\\Users\\[^\\]+", RegexOptions.IgnoreCase)]
    private static partial Regex UserPathNoSlashPattern();

    private static readonly string[] SensitiveKeywords =
    [
        "password", "passwd", "secret", "token", "apikey", "api_key",
        "credential", "auth", "bearer", "private"
    ];

    /// <summary>
    /// Returns a sanitized copy of the stack trace safe to send in a crash report.
    /// Replaces user home paths and truncates to MaxLength characters.
    /// </summary>
    public static string Sanitize(string? stackTrace)
    {
        if (string.IsNullOrEmpty(stackTrace))
            return "(no stack trace)";

        // Replace C:\Users\<name>\ with %USERDIR%\
        string sanitized = UserPathPattern().Replace(stackTrace, @"%USERDIR%\");
        sanitized = UserPathNoSlashPattern().Replace(sanitized, "%USERDIR%");

        // Replace home directory on non-Windows paths too (e.g. /home/username/)
        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(homeDir))
            sanitized = sanitized.Replace(homeDir, "%USERDIR%", StringComparison.OrdinalIgnoreCase);

        // Strip lines that contain sensitive keywords
        var lines = sanitized.Split('\n');
        var filtered = new System.Text.StringBuilder();
        foreach (var line in lines)
        {
            bool hasSensitiveWord = false;
            foreach (var keyword in SensitiveKeywords)
            {
                if (line.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    hasSensitiveWord = true;
                    break;
                }
            }
            if (!hasSensitiveWord)
            {
                filtered.AppendLine(line);
            }
        }

        sanitized = filtered.ToString();

        if (sanitized.Length > MaxLength)
            sanitized = sanitized[..MaxLength] + "\n... (truncated)";

        return sanitized.Trim();
    }
}
