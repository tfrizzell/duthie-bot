namespace Duthie.Types.Leagues;

public class LeagueState
{
    public Guid LeagueId { get; set; }
    public string? LastBid { get; set; }

#nullable disable
    internal virtual League League { get; set; }
#nullable enable
}