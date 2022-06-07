namespace Duthie.Types.Guilds;

public class GuildMessageEmbed
{
    public bool ShowAuthor { get; set; } = false;
    public uint? Color { get; set; }
    public string? Title { get; set; }
    public string? Thumbnail { get; set; }
    public string Content { get; set; } = "";
    public string? Footer { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public string? Url { get; set; }
}