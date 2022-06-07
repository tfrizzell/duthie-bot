using System.Text.RegularExpressions;
using Discord;

namespace Duthie.Bot.Utils;

public static class MessageUtils
{
    public static string[]? Chunk(string? message = null, int chunkSize = DiscordConfig.MaxMessageSize)
    {
        if (message == null)
            return null;

        var chunks = new List<string>();

        while (message.Length > chunkSize)
            chunks.Add(message.Substring(chunks.Count() * chunkSize, chunkSize));

        return chunks.ToArray();
    }

    public static string Escape(string text) =>
        Regex.Replace(text, @"[*_~`]", @"\$0");

    public static string Pluralize(int value, string text)
    {
        if (value == 1)
            return $"{value} {text}";

        if (text.EndsWith("y"))
            return $"{value} {Regex.Replace(text, "y$", "")}";
        else if (text.EndsWith("s"))
            return $"{value} {text}es";
        else
            return $"{value} {text}s";
    }
}