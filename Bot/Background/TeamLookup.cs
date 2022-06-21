using Duthie.Types.Leagues;
using Duthie.Types.Sites;
using Duthie.Types.Teams;

namespace Duthie.Bot.Background;

internal sealed class TeamLookup
{
    private readonly Dictionary<Guid, Dictionary<string, Team>> _siteTeamLookup;
    private readonly Dictionary<Guid, Dictionary<string, Team>> _leagueTeamLookup;

    public TeamLookup(IEnumerable<League> leagues)
    {
        _siteTeamLookup = leagues.GroupBy(l => l.SiteId)
            .ToDictionary(g => g.Key, g => g.SelectMany(l => l.Teams)
                .GroupBy(t => t.ExternalId)
                .ToDictionary(t => t.Key, t => t.First().Team));

        _leagueTeamLookup = leagues.GroupBy(l => l.Id)
            .ToDictionary(g => g.Key, g => g.SelectMany(l => l.Teams)
                .GroupBy(t => t.ExternalId)
                .ToDictionary(t => t.Key, t => t.First().Team));
    }

    public Team Get(League league, string externalId)
    {
        Team? team = null;
        _leagueTeamLookup.TryGetValue(league.Id, out var leagueTeams);
        leagueTeams?.TryGetValue(externalId, out team);

        if (team == null)
            throw new KeyNotFoundException($"no team with external id {externalId} was found for league \"{league.Name}\" [{league.Id}]");

        return team;
    }

    public Team Get(Site site, string externalId)
    {
        Team? team = null;
        _siteTeamLookup.TryGetValue(site.Id, out var siteTeams);
        siteTeams?.TryGetValue(externalId, out team);

        if (team == null)
            throw new KeyNotFoundException($"no team with external id {externalId} was found for site \"{site.Name}\" [{site.Id}]");

        return team;
    }

    public bool Has(League league, string externalId)
    {
        _siteTeamLookup.TryGetValue(league.Id, out var leagueTeams);
        return leagueTeams?.ContainsKey(externalId) == true;
    }

    public bool Has(Site site, string externalId)
    {
        _siteTeamLookup.TryGetValue(site.Id, out var siteTeams);
        return siteTeams?.ContainsKey(externalId) == true;
    }
}