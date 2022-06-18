namespace Duthie.Types.Guilds;

public class GuildMessage
{
    public Guid Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public uint? Color { get; set; }
    public string? Title { get; set; }
    public string? Thumbnail { get; set; }
    public string Content { get; set; } = "";
    public string? Footer { get; set; }
    public string? Url { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }

#nullable disable
    public virtual Guild Guild { get; set; }
#nullable enable
}