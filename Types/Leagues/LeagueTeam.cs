using Duthie.Types.Teams;

namespace Duthie.Types.Leagues;

public class LeagueTeam
{
    public Guid LeagueId { get; set; }
    public Guid TeamId { get; set; }
    public string ExternalId { get; set; } = "";

#nullable disable
    public virtual League League { get; set; }
    public virtual Team Team { get; set; }
#nullable enable
}