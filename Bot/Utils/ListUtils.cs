using System.Text.RegularExpressions;

namespace Duthie.Bot.Utils;

public static class ListUtils
{
    public const int STYLE_BOLD = 1;
    public const int STYLE_ITALIC = 2;
    public const int STYLE_UNDERLINE = 4;
    public const int STYLE_STRIKETHROUGH = 8;

    public static IEnumerable<string> DrawBox(IEnumerable<IEnumerable<string>> data, IEnumerable<string>? headers = null)
    {
        var rows = headers == null ? data : data.Concat(new List<IEnumerable<string>>() { headers });
        var widths = Enumerable.Range(0, rows.Max(row => row.Count()))
            .Select(i => rows.Max(row => row.ElementAtOrDefault(i)?.Trim()?.Length ?? 0))
            .ToArray();
        var format = $"│ {string.Join(" │ ", widths.Select((width, i) => $"{{{i},-{width}}}"))} │";
        var messages = new List<string>();

        var header = string.Join("\n", new string[] {
            "┌" + string.Join("┬", widths.Select(width => new String('─', width + 2))) + "┐",
        }.Concat(headers == null
            ? new string[] { }
            : new string[] {
                headers == null ? "" : string.Format(format, headers.ToArray()),
                headers == null ? "" : "├" + string.Join("┼", widths.Select(width => new String('─', width + 2))) + "┤",
            }));

        var footer = string.Join("\n", new string[] {
            "└" + string.Join("┴", widths.Select(width => new String('─', width + 2))) + "┘"});

        var buffer = header;

        foreach (var row in data)
        {
            var buf = string.Format(format, row.ToArray());

            if (ExceedsCharacterLimit(buffer.Length + buf.Length))
            {
                messages.Add($"```\n{buffer}\n```");
                buffer = buf;
            }
            else if (buffer.Length > 0)
                buffer = $"{buffer}\n{buf}";
            else
                buffer = buf;
        }

        if (ExceedsCharacterLimit(buffer.Length + footer.Length))
        {
            var lines = buffer.Split("\n");
            messages.Add($"```\n{string.Join("\n", lines.SkipLast(1))}\n```");
            buffer = lines.Last();
        }

        messages.Add($"```{buffer}\n{footer}\n```");
        return messages;
    }

    private static bool ExceedsCharacterLimit(int length) =>
        MessageUtils.ExceedsCharacterLimit(length + 10);
}