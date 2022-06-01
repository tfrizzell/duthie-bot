namespace Duthie.Types;

public class Watcher
{
    public Guid Id { get; set; }
    public ulong GuildId { get; set; }
    public Guid LeagueId { get; set; }
    public Guid TeamId { get; set; }
    public WatcherType Type { get; set; }
    public ulong? ChannelId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; } = null;
    
    public virtual Guild Guild { get; set; }
    public virtual League League { get; set; }
    public virtual Team Team { get; set; }
}