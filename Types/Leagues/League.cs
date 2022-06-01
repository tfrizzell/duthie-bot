namespace Duthie.Types;

public class League
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Name { get; set; }
    public IReadOnlyCollection<string> Tags { get; set; }
    public bool Enabled { get; set; } = true;

    public virtual Site Site { get; set; }
    public virtual IReadOnlyCollection<LeagueTeam> LeagueTeams { get; set; }
    public virtual IEnumerable<Team> Teams { get; set; }
}