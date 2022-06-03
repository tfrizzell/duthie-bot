using Duthie.Types.Guilds;

namespace Duthie.Types;

public class GuildMessage
{
    public Guid Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong ChannelId { get; set; }
    public string Message { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }

#nullable disable
    public virtual Guild Guild { get; set; }
#nullable enable
}