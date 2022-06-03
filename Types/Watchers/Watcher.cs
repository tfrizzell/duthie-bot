using Duthie.Types.Guilds;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;

namespace Duthie.Types.Watchers;

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

#nullable disable
    public virtual Guild Guild { get; set; }
    public virtual League League { get; set; }
    public virtual Team Team { get; set; }
#nullable enable
}