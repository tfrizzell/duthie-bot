namespace Duthie.Types;

public class Guild
{
    public ulong Id { get; set; }
    public string Name { get; set; } = "";
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LeftAt { get; set; } = null;
}