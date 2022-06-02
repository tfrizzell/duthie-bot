using System.Text.RegularExpressions;
using System.Web;
using Duthie.Types;
using Duthie.Types.Api;

namespace Duthie.Modules.LeagueGaming;

public class LeagueGamingApi : ILeagueInfoApi, ITeamsApi
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

    private string GetUrl(string file = "index.php", string path = "leaguegaming/league", IDictionary<string, object?>? parameters = null)
    {
        var queryString = parameters == null ? string.Empty
            : string.Join("&", parameters.Where(p => p.Value != null).Select(p => string.Join("=", HttpUtility.UrlEncode(p.Key), HttpUtility.UrlEncode(p.Value!.ToString()))));
        return Regex.Replace($"https://www.leaguegaming.com/forums/{file}?{path}&{queryString}".Replace("?&", "?"), @"[?&]+$", "");
    }

    public async Task<ILeague?> GetLeagueInfoAsync(League league)
    {
        if (league.Info is not LeagueGamingLeagueInfo)
            return null;

        var leagueInfo = (league.Info as LeagueGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(
            parameters: new Dictionary<string, object?>
            {
                ["action"] = "league",
                ["page"] = "standing",
                ["leagueid"] = leagueInfo.LeagueId,
                ["seasonid"] = 1
            }));

        var info = Regex.Match(html,
            @$"<li[^>]*\bcustom-tab-{leagueInfo.LeagueId}\b[^>]*>\s*<a[^>]*/league\.(\d+)[^>]*>\s*<span[^>]*>(.*?)</span>\s*</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!info.Success)
            return null;

        var season = Regex.Match(html,
            @$"<a[^>]*leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid=(\d+)[^>]*>Roster</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

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

        var html = await _httpClient.GetStringAsync(GetUrl(
            parameters: new Dictionary<string, object?>
            {
                ["action"] = "league",
                ["page"] = "standing",
                ["leagueid"] = leagueInfo.LeagueId,
                ["seasonid"] = leagueInfo.SeasonId
            }));

        var nameMatches = Regex.Matches(html,
            @$"<div[^>]*\bteam_box_icon\b[^>]*>\s*<a[^>]*page=team_page&(?:amp;)?teamid=(\d+)&(?:amp;)?leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid={leagueInfo.SeasonId}[^>]*>(.*?)</a>\s*</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

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

        var shortNameMatches = Regex.Matches(html,
            @$"<td[^>]*><img[^>]*/team\d+.png[^>]*> \d+\) .*?\*?<a[^>]*page=team_page&(?:amp;)?teamid=(\d+)&(?:amp;)?leagueid=(?:{leagueInfo.LeagueId})?&(?:amp;)?seasonid=(?:{leagueInfo.SeasonId})?[^>]*>(.*?)</a>.*?</td>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

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