namespace Duthie.Types;

public class LeagueTeam
{
    public Guid LeagueId { get; set; }
    public Guid TeamId { get; set; }
    public string IId { get; set; }

    public virtual League League { get; set; }
    public virtual Team Team { get; set; }
}