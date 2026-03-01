using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Birdsoft.Infrastructure.Logging;

public static class LogEntryRedactor
{
    private static readonly string[] DefaultPatterns =
    [
        "(?i)(access_token\\s*[=:]\\s*)([^&\\s]+)",
        "(?i)(refresh_token\\s*[=:]\\s*)([^&\\s]+)",
        "(?i)(client_secret\\s*[=:]\\s*)([^&\\s]+)",
        "(?i)(password\\s*[=:]\\s*)([^&\\s]+)",
        "(?i)(pwd\\s*[=:]\\s*)([^&\\s]+)",
        "(?i)(bearer\\s+)([A-Za-z0-9_\\-\\.]+)",
        "(?i)(https?://[^\\s]*oauth[^\\s]*)([A-Za-z0-9_\\-\\.=%&]+)"
    ];

    private static readonly ConcurrentBag<Regex> Patterns = new();

    static LogEntryRedactor()
    {
        ResetToDefaults();
    }

    public static void AddPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return;
        }

        Patterns.Add(new Regex(pattern, RegexOptions.Compiled));
    }

    public static void ResetToDefaults()
    {
        while (Patterns.TryTake(out _))
        {
        }

        foreach (var pattern in DefaultPatterns)
        {
            Patterns.Add(new Regex(pattern, RegexOptions.Compiled));
        }
    }

    public static string? Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var output = value;
        foreach (var regex in Patterns)
        {
            output = regex.Replace(output, "$1[REDACTED]");
        }

        return output;
    }

    public static IReadOnlyDictionary<string, object?> RedactProperties(IReadOnlyDictionary<string, object?> properties)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in properties)
        {
            normalized[kv.Key] = NormalizeAndRedactValue(kv.Value);
        }

        return normalized;
    }

    private static object? NormalizeAndRedactValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return value switch
        {
            string s => Redact(s),
            bool => value,
            byte or sbyte or short or ushort or int or uint or long or ulong => value,
            float or double or decimal => value,
            DateTime or DateTimeOffset or Guid => value,
            _ => Redact(value.ToString())
        };
    }
}