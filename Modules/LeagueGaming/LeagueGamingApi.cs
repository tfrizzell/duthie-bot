using System.Text.RegularExpressions;
using System.Web;
using Duthie.Types;
using Duthie.Types.Api;

namespace Duthie.Modules.LeagueGaming;

public class LeagueGamingApi : ISiteApi
{
    private readonly HttpClient _httpClient;

    public LeagueGamingApi()
    {
        _httpClient = new HttpClient();
    }

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { LeagueGamingSiteProvider.SITE_ID };
    }

    private string GetUrl(IDictionary<string, object> parameters, string file = "index.php", string path = "leaguegaming/league")
    {
        var queryString = string.Join("&", parameters.Select(p => string.Join("=", HttpUtility.UrlEncode(p.Key), HttpUtility.UrlEncode(p.Value.ToString()))));
        return $"https://www.leaguegaming.com/forums/{file}?{path}&{queryString}".Replace("?&", "?");
    }

    public async Task<ILeague?> GetLeagueInfoAsync(League league)
    {
        if (league.Info is not LeagueGamingLeagueInfo)
            return null;

        var leagueInfo = (league.Info as LeagueGamingLeagueInfo)!;

        var html = Regex.Replace(await _httpClient.GetStringAsync(GetUrl(new Dictionary<string, object>
        {
            ["action"] = "league",
            ["page"] = "standing",
            ["leagueid"] = leagueInfo.LeagueId,
            ["seasonid"] = 1
        })), @"[\r\n]+", "");

        var info = Regex.Match(html, @$"<li[^>]* custom-tab-{leagueInfo.LeagueId} [^>]*>.*?<a[^>]*/league\.(\d+)[^>]*>.*?<span[^>]*>(.*?)</span>.*?</a>", RegexOptions.IgnoreCase);

        if (!info.Success)
            return null;

        var season = Regex.Match(html, @$"<a[^>]*leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid=(\d+)[^>]*>Roster</a>", RegexOptions.IgnoreCase);

        return new League
        {
            Name = info.Groups[2].Value.Trim(),
            Info = new LeagueGamingLeagueInfo
            {
                LeagueId = leagueInfo.LeagueId,
                SeasonId = season.Success ? int.Parse(season.Groups[1].Value) : leagueInfo.SeasonId,
                ForumId = int.Parse(info.Groups[1].Value)
            }
        };
    }

    public async Task<IEnumerable<LeagueTeam>?> GetTeamsAsync(League league)
    {
        if (league.Info is not LeagueGamingLeagueInfo)
            return null;

        var leagueInfo = (league.Info as LeagueGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(new Dictionary<string, object>
        {
            ["action"] = "league",
            ["page"] = "standing",
            ["leagueid"] = leagueInfo.LeagueId,
            ["seasonid"] = leagueInfo.SeasonId
        }));

        var nameMatches = Regex.Matches(html, @$"<div[^>]*class=""team_box_icon""[^>]*>.*?<a[^>]*page=team_page&(?:amp;)?teamid=(\d+)&(?:amp;)?leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid={leagueInfo.SeasonId}[^>]*>(.*?)</a>.*?</div>", RegexOptions.IgnoreCase);

        if (nameMatches.Count() == 0)
            return null;

        var teams = new Dictionary<string, LeagueTeam>();

        foreach (Match match in nameMatches)
        {
            var id = match.Groups[1].Value;

            if (!teams.ContainsKey(id))
                teams.Add(id, new LeagueTeam { LeagueId = league.Id, League = league, Team = new Team() });

            teams[id].Team.Name =
            teams[id].Team.ShortName = match.Groups[2].Value.Trim();
            teams[id].IId = id;
        }

        var shortNameMatches = Regex.Matches(html, @$"<td[^>]*><img[^>]*/team\d+.png[^>]*> \d+\) .*?\*?<a[^>]*page=team_page&(?:amp;)?teamid=(\d+)&(?:amp;)?leagueid=(?:{leagueInfo.LeagueId})?&(?:amp;)?seasonid=(?:{leagueInfo.SeasonId})?[^>]*>(.*?)</a>.*?</td>", RegexOptions.IgnoreCase);

        if (shortNameMatches.Count() > 0)
        {
            foreach (Match match in shortNameMatches)
            {
                var id = match.Groups[1].Value;

                if (!teams.ContainsKey(id))
                    teams.Add(id, new LeagueTeam { LeagueId = league.Id, League = league, Team = new Team() });

                teams[id].Team.ShortName = match.Groups[2].Value.Trim();
                teams[id].IId = id;

                if (string.IsNullOrWhiteSpace(teams[id].Team.Name))
                    teams[id].Team.Name = teams[id].Team.ShortName;
            }
        }

        return teams.Values.ToList();
    }
}