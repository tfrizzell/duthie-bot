using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Duthie.Types.Api;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;

namespace Duthie.Modules.MyVirtualGaming;

public class MyVirtualGamingApi : ILeagueInfoApi, ITeamsApi
{
    private static readonly TimeZoneInfo Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private readonly HttpClient _httpClient = new HttpClient();

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { MyVirtualGamingSiteProvider.SITE_ID };
    }

    private string GetUrl(string league = "vghl", string path = "", IDictionary<string, object?>? parameters = null)
    {
        var queryString = parameters == null ? string.Empty
            : string.Join("&", parameters.Where(p => p.Value != null).Select(p => string.Join("=", HttpUtility.UrlEncode(p.Key), HttpUtility.UrlEncode(p.Value!.ToString()))));
        return Regex.Replace($"https://vghl.myvirtualgaming.com/vghlleagues/{league}/{path}?{queryString}".Replace("?&", "?"), @"[?&]+$", "");
    }

    public async Task<ILeague?> GetLeagueInfoAsync(League league)
    {
        if (!Supports.Contains(league.SiteId) || league.Info is not MyVirtualGamingLeagueInfo)
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        var xml = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: leagueInfo.LeagueId,
            parameters: new Dictionary<string, object?>
            {
                ["format"] = "feed",
                ["type"] = "atom"
            }));

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var title = doc.GetElementsByTagName("title")[0];
        var id = doc.GetElementsByTagName("id")[0];

        if (title == null || id == null)
            return null;

        var html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "schedule"));

        var season = Regex.Match(
            Regex.Match(html,
                @"<select[^>]*\bsingle_seasons\b[^>]*>(.*?)</select>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value,
            @"<option(?=[^>]*selected)[^>]*value=[""']?(\d+)[""']?[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return new League
        {
            Name = Regex.Replace(title.InnerText.Trim(), @"\s+Home$", ""),
            Info = new MyVirtualGamingLeagueInfo
            {
                LeagueId = Regex.Split(id.InnerText.Trim(), @"/+")[3] ?? leagueInfo.LeagueId,
                SeasonId = season.Success ? int.Parse(season.Groups[1].Value) : leagueInfo.SeasonId,
            }
        };
    }

    public async Task<IEnumerable<LeagueTeam>?> GetTeamsAsync(League league)
    {
        if (!Supports.Contains(league.SiteId) || league.Info is not MyVirtualGamingLeagueInfo)
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "player-statistics",
            parameters: new Dictionary<string, object?>
            {
                ["filter_schedule"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null
            }));

        var nameMatches = Regex.Matches(
            Regex.Match(html,
                @"<select[^>]*\bfilter_stat_team\b[^>]*>(.*?)</select>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value,
            @"<option[^>]*value=[""']?(\d+)[""']?[^>]*>(.*?)</option>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (nameMatches.Count() == 0)
            return null;

        var map = await GetTeamMapAsync(league);
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

        html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "schedule",
            parameters: new Dictionary<string, object?>
            {
                ["filter_schedule"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null
            }));

        var shortNameMatches = Regex.Matches(html,
            @$"<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(\w+).png[^>]*>\s*</div>\s*<div[^>]*\bschedule-team\b[^>]*>\s*<div[^>]*\bschedule-team-name\b[^>]*>(.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .DistinctBy(m => m.Groups[1].Value);

        if (shortNameMatches.Count() > 0)
        {
            foreach (Match match in shortNameMatches)
            {
                if (!map.ContainsKey(match.Groups[1].Value))
                    continue;

                var id = map[match.Groups[1].Value];

                if (!teams.ContainsKey(id))
                    teams.Add(id, new LeagueTeam { LeagueId = league.Id, League = league, Team = new Team() });

                teams[id].Team.ShortName = match.Groups[2].Value.Trim();
                teams[id].IId = id;

                if (string.IsNullOrWhiteSpace(teams[id].Team.Name))
                    teams[id].Team.Name = teams[id].Team.ShortName;
            }
        }

        return teams.Values
            .Where(t => map.Values.Contains(t.IId))
            .Select(t =>
            {
                FixTeam(t.Team);
                return t;
            })
            .ToList();
    }

    private async Task<IDictionary<string, string>> GetTeamMapAsync(League league)
    {
        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "rosters",
            parameters: new Dictionary<string, object?>
            {
                ["scheduleId "] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null
            }));

        return Regex.Matches(html,
            @"<a[^>]*/rosters\?id=(\d+)[^>]*>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*<\/a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .DistinctBy(m => m.Groups[2].Value)
        .ToDictionary(m => m.Groups[2].Value, m => m.Groups[1].Value);
    }

    private Team FixTeam(Team team)
    {
        switch (team.Name.Trim())
        {
            case "Nashville Nashville":
                team.Name = "Nashville Predators";
                team.ShortName = "Predators";
                break;
        }

        return team;
    }
}