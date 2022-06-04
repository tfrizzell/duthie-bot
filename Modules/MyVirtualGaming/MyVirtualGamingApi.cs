using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Duthie.Types.Api;
using Duthie.Types.Games;
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

    public async Task<IEnumerable<ApiGame>?> GetGamesAsync(League league)
    {
        if (!Supports.Contains(league.SiteId) || league.Info is not MyVirtualGamingLeagueInfo)
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "schedule",
            parameters: new Dictionary<string, object?>
            {
                ["single_seasons"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
            }));

        var weeks = Regex.Matches(html,
            @"<option[^>]*value=[""']?(\d{8})[""']?[^>]*>\d{4}-\d{2}-\d{2}</option>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Select(m => m.Groups[1].Value);

        if (weeks.Count() == 0)
            return new List<ApiGame>();

        var teams = await GetTeamMapAsync(league);

        return (await Task.WhenAll(weeks.Select(async week =>
        {
            var _html = await _httpClient.GetStringAsync(GetUrl(
                league: leagueInfo.LeagueId,
                path: "schedule",
                parameters: new Dictionary<string, object?>
                {
                    ["single_seasons"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
                    ["filter_scheduled_week"] = week,
                }));

            var matches = Regex.Matches(_html,
                @$"(?:{string.Join("|",
                    @"(\d+.{2} \S+ \d{4} @ \d+:\d+[ap]m)",
                    @"<div[^>]*\bgame_div_(\d+)\b[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(\w+)\.png[^>]*>\s*</div>\s*<div[^>]*>.*?</div>\s*<div[^>]*\bschedule-team-score\b[^>]*>\s*(\d+|-)\s*</div>\s*</div>\s*<div[^>]*>\s*<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(\w+)\.png[^>]*>\s*</div>\s*<div[^>]*>.*?</div>\s*<div[^>]*\bschedule-team-score\b[^>]*>\s*(\d+|-)\s*</div>\s*</div>\s*<div[^>]*>.*?</div>\s*</div>\s*<div[^>]*\bschedule-summary-link\b[^>]*>\s*<a[^>]*>(Final|Stats)(?:/(OT|SO))?</a>\s*</div>")})",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (matches.Count() == 0)
                return new List<ApiGame>();

            DateTimeOffset? date = null;
            var games = new List<ApiGame>();

            foreach (Match match in matches)
            {
                if (!string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    var parsed = DateTime.Parse(Regex.Replace(match.Groups[1].Value, @"^(\d+).{2} (\S+) (\d+) @ (.*?)", @"$2 $1, $3 $4"));
                    date = new DateTimeOffset(parsed, Timezone.GetUtcOffset(parsed));
                    continue;
                }

                if (date == null)
                    continue;

                games.Add(new ApiGame
                {
                    LeagueId = league.Id,
                    GameId = match.Groups[2].Value.Trim(),
                    Date = date.GetValueOrDefault(),
                    VisitorIId = teams[match.Groups[3].Value.Trim()],
                    VisitorScore = int.TryParse(match.Groups[4].Value, out var visitorScore) ? visitorScore : null,
                    HomeIId = teams[match.Groups[5].Value.Trim()],
                    HomeScore = int.TryParse(match.Groups[6].Value, out var homeScore) ? homeScore : null,
                    Overtime = match.Groups[8].Value.ToUpper().Contains("OT"),
                    Shootout = match.Groups[8].Value.ToUpper().Contains("SO"),
                });
            }

            return games;
        }))).SelectMany(g => g);
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
                ["type"] = "atom",
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
                ["filter_schedule"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
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

        var teams = nameMatches
            .DistinctBy(m => m.Groups[1].Value)
            .ToDictionary(
                m => m.Groups[1].Value,
                m => new LeagueTeam
                {
                    LeagueId = league.Id,
                    League = league,
                    Team = new Team
                    {
                        Name = m.Groups[2].Value.Trim(),
                        ShortName = m.Groups[2].Value.Trim(),
                    },
                    IId = m.Groups[1].Value,
                });

        html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "schedule",
            parameters: new Dictionary<string, object?>
            {
                ["filter_schedule"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
            }));

        var shortNameMatches = Regex.Matches(html,
            @$"<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(\w+).png[^>]*>\s*</div>\s*<div[^>]*\bschedule-team\b[^>]*>\s*<div[^>]*\bschedule-team-name\b[^>]*>(.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .DistinctBy(m => m.Groups[1].Value);

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
                ["scheduleId"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
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