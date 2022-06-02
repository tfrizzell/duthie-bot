namespace Duthie.Types;

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
    public virtual IEnumerable<Team> Teams { get; set; }
#nullable enable
}

public interface ILeague
{
    string Name { get; set; }
    object? Info { get; set; }
}