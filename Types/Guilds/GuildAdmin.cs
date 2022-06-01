namespace Duthie.Types;

public class GuildAdmin
{
    public ulong GuildId { get; set; }
    public ulong MemberId { get; set; }

#nullable disable
    public virtual Guild Guild { get; set; }
#nullable enable
}