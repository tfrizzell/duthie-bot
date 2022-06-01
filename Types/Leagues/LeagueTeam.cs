namespace Duthie.Types;

public class LeagueTeam
{
    public Guid LeagueId { get; set; }
    public Guid TeamId { get; set; }
    public string IId { get; set; } = "";

#nullable disable
    public virtual League League { get; set; }
    public virtual Team Team { get; set; }
#nullable enable
}