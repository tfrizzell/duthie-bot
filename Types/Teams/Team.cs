namespace Duthie.Types;

public class Team
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public Tags Tags { get; set; } = new Tags();

#nullable disable
    public virtual IReadOnlyCollection<LeagueTeam> LeagueTeams { get; set; }
    public virtual IReadOnlyCollection<League> Leagues => LeagueTeams?.Select(m => m.League).ToList();
#nullable enable
}