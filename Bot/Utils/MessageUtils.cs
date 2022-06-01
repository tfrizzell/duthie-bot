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
}