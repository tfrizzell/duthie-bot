namespace Duthie.Types.Guilds;

public class Guild
{
    public ulong Id { get; set; }
    public string Name { get; set; } = "";
    public ulong DefaultChannelId { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; } = null;
}