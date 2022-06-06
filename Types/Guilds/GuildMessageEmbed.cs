namespace Duthie.Types.Guilds;

public class GuildMessageEmbed
{
    public string? Title { get; set; }
    public string Content { get; set; } = "";
    public string? Footer { get; set; }
    public string? Url { get; set; }
    public string? Thumbnail { get; set; }
    public uint? Color { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}