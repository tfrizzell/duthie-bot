using System.Text.RegularExpressions;

namespace Duthie.Bot.Utils;

public static class MessageUtils
{
    public const int MAX_MESSAGE_LENGTH = 2000;

    public static string[]? Chunk(string? message = null, int chunkSize = MAX_MESSAGE_LENGTH)
    {
        if (message == null)
            return null;

        var chunks = new List<string>();

        while (message.Length > chunkSize)
            chunks.Add(message.Substring(chunks.Count() * chunkSize, chunkSize));

        return chunks.ToArray();
    }

    public static bool ExceedsCharacterLimit(int length) =>
        length > MessageUtils.MAX_MESSAGE_LENGTH;

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