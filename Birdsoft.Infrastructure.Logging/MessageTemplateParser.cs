using System.Text;
using System.Text.RegularExpressions;

namespace Birdsoft.Infrastructure.Logging;

public static class MessageTemplateParser
{
    private static readonly Regex PlaceholderRegex = new("\\{(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:[^}]*)\\}", RegexOptions.Compiled);

    public static MessageTemplateParseResult Parse(string messageTemplate, params object?[] args)
    {
        if (string.IsNullOrWhiteSpace(messageTemplate))
        {
            return new MessageTemplateParseResult(string.Empty, new Dictionary<string, object?>());
        }

        var properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var matches = PlaceholderRegex.Matches(messageTemplate);

        for (var index = 0; index < matches.Count; index++)
        {
            var key = matches[index].Groups["name"].Value;
            var value = index < args.Length ? args[index] : null;
            if (!properties.ContainsKey(key))
            {
                properties[key] = value;
            }
        }

        var rendered = Render(messageTemplate, args);
        return new MessageTemplateParseResult(rendered, properties);
    }

    private static string Render(string messageTemplate, object?[] args)
    {
        if (args.Length == 0)
        {
            return messageTemplate;
        }

        var builder = new StringBuilder(messageTemplate);
        var matches = PlaceholderRegex.Matches(messageTemplate);
        var offset = 0;

        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var replacement = index < args.Length ? args[index]?.ToString() ?? string.Empty : string.Empty;

            builder.Remove(match.Index + offset, match.Length);
            builder.Insert(match.Index + offset, replacement);
            offset += replacement.Length - match.Length;
        }

        return builder.ToString();
    }
}

public sealed record MessageTemplateParseResult(string RenderedMessage, IReadOnlyDictionary<string, object?> Properties);