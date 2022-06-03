using Duthie.Types.Common;
using Duthie.Types.Sites;
using Duthie.Types.Teams;

namespace Duthie.Types.Leagues;

public class League : ILeague
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Name { get; set; } = "";
    public object? Info { get; set; } = null;
    public Tags Tags { get; set; } = new Tags();
    public bool Enabled { get; set; } = true;

#nullable disable
    public virtual Site Site { get; set; }
    public virtual IEnumerable<LeagueTeam> LeagueTeams { get; set; }
    public virtual IEnumerable<Team> Teams => LeagueTeams?.Select(t => t.Team);
#nullable enable
}

public interface ILeague
{
    string Name { get; set; }
    object? Info { get; set; }
}