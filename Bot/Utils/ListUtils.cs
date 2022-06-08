using System.Text.RegularExpressions;
using Discord;

namespace Duthie.Bot.Utils;

public static class ListUtils
{
    public static IEnumerable<Embed> CreateEmbeddedTable(IEnumerable<IEnumerable<string>> data, IEnumerable<string>? headers = null, string? title = null) =>
        CreateTable(data, headers, EmbedBuilder.MaxDescriptionLength)
            .Select((message, i) => new EmbedBuilder().WithTitle(i == 0 ? title : null).WithDescription(message).Build());

    public static IEnumerable<string> CreateTable(IEnumerable<IEnumerable<string>> data, IEnumerable<string>? headers = null, int maxSize = DiscordConfig.MaxMessageSize)
    {
        var messages = new List<string>();
        var numCols = new int[] { headers?.Count() ?? 0 }.Concat(data.Select(row => row.Count())).Max();

        var rows = headers == null ? data : data.Concat(new List<IEnumerable<string>>() { headers });
        var widths = Enumerable.Range(0, rows.Max(row => row.Count()))
            .Select(i => rows.Max(row => row.ElementAtOrDefault(i)?.Trim()?.Length ?? 0))
            .ToArray();
        var format = $"│ {string.Join(" │ ", widths.Select((width, i) => $"{{{i},-{width}}}"))} │";

        var header = string.Join("\n", new string[] {
            "┌" + string.Join("┬", widths.Select(width => new String('─', width + 2))) + "┐",
        }.Concat(headers == null
            ? new string[] { }
            : new string[] {
                string.Format(format, Enumerable.Range(0, numCols).Select((_, i) => headers?.ElementAtOrDefault(i)?.Trim() ?? "").ToArray()),
                "├" + string.Join("┼", widths.Select(width => new String('─', width + 2))) + "┤",
            }));

        var footer = string.Join("\n", new string[] {
            "└" + string.Join("┴", widths.Select(width => new String('─', width + 2))) + "┘"});

        var buffer = header;

        foreach (var row in data)
        {
            var buf = string.Format(format, row.ToArray());

            if (buffer.Length + buf.Length + 10 > maxSize)
            {
                messages.Add($"```\n{buffer}\n```");
                buffer = buf;
            }
            else if (buffer.Length > 0)
                buffer = $"{buffer}\n{buf}";
            else
                buffer = buf;
        }

        if (buffer.Length + footer.Length + 10 > maxSize)
        {
            var lines = buffer.Split("\n");
            messages.Add($"```\n{string.Join("\n", lines.SkipLast(1))}\n```");
            buffer = lines.Last();
        }

        messages.Add($"```{buffer}\n{footer}\n```");
        return messages;
    }
}