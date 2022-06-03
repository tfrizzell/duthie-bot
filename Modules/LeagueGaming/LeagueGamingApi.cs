using System.Text.RegularExpressions;
using System.Web;
using Duthie.Types.Api;
using Duthie.Types.Games;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;

namespace Duthie.Modules.LeagueGaming;

public class LeagueGamingApi
    : IGamesApi, ILeagueInfoApi, ITeamsApi
{
    private static readonly TimeZoneInfo Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private readonly HttpClient _httpClient = new HttpClient();

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

    public async Task<IEnumerable<ApiGame>?> GetGamesAsync(League league)
    {
        if (!Supports.Contains(league.SiteId) || league.Info is not LeagueGamingLeagueInfo)
            return null;

        var leagueInfo = (league.Info as LeagueGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(
            parameters: new Dictionary<string, object?>
            {
                ["action"] = "league",
                ["page"] = "league_schedule_all",
                ["leagueid"] = leagueInfo.LeagueId,
                ["seasonid"] = leagueInfo.SeasonId,
            }));

        var matches = Regex.Matches(html,
            @$"(?:{string.Join("|",
                @"<h4[^>]*sh4[^>]*>(.*?)</h4>*",
                @"<span[^>]*sweekid[^>]*>Week\s*(\d+)</span>\s*(?:<span[^>]*sgamenumber[^>]*>Game\s*#\s*(\d+)</span>)?\s*<img[^>]*/team(\d+)\.png[^>]*>\s*<a[^>]*&gameid=(\d+)[^>]*>\s*<span[^>]*steamname[^>]*>(.*?)</span>\s*<span[^>]*sscore[^>]*>(vs|(\d+)\D+(\d+))</span>\s*<span[^>]*steamname[^>]*>(.*?)</span>\s*</a>\s*<img[^>]*/team(\d+)\.png[^>]*>")})",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (matches.Count() == 0)
            return new List<ApiGame>();

        var games = new List<ApiGame>();
        DateTimeOffset? date = null;

        foreach (Match match in matches)
        {
            if (!string.IsNullOrWhiteSpace(match.Groups[1].Value))
            {
                var parsed = DateTime.Parse(Regex.Replace(match.Groups[1].Value, @"(\d+)[\S\D]+", @"$1"));

                var present = new DateTimeOffset(parsed, Timezone.GetUtcOffset(parsed));
                var dPresent = Math.Abs((DateTimeOffset.UtcNow - present).TotalMilliseconds);

                var past = present.AddYears(-1);
                var dPast = Math.Abs((DateTimeOffset.UtcNow - past).TotalMilliseconds);

                var future = present.AddYears(1);
                var dFuture = Math.Abs((DateTimeOffset.UtcNow - future).TotalMilliseconds);

                if (dFuture < dPast && dFuture < dPresent)
                    date = future;
                else if (dPast < dPresent)
                    date = past;
                else
                    date = present;

                continue;
            }

            if (date == null)
                continue;

            games.Add(new ApiGame
            {
                LeagueId = league.Id,
                GameId = match.Groups[5].Value.Trim(),
                Date = date.GetValueOrDefault(),
                VisitorIId = match.Groups[4].Value.Trim(),
                VisitorScore = int.TryParse(match.Groups[8].Value, out var visitorScore) ? visitorScore : null,
                HomeIId = match.Groups[11].Value.Trim(),
                HomeScore = int.TryParse(match.Groups[9].Value, out var homeScore) ? homeScore : null,
            });
        }

        return games;
    }

    public async Task<ILeague?> GetLeagueInfoAsync(League league)
    {
        if (!Supports.Contains(league.SiteId) || league.Info is not LeagueGamingLeagueInfo)
            return null;

        var leagueInfo = (league.Info as LeagueGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(
            parameters: new Dictionary<string, object?>
            {
                ["action"] = "league",
                ["page"] = "standing",
                ["leagueid"] = leagueInfo.LeagueId,
                ["seasonid"] = 1,
            }));

        var info = Regex.Match(html,
            @$"<li[^>]*\bcustom-tab-{leagueInfo.LeagueId}\b[^>]*>\s*<a[^>]*/league\.(\d+)[^>]*.*?<span[^>]*>(.*?)</span>.*?</a>",
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
                ForumId = int.Parse(info.Groups[1].Value),
            }
        };
    }

    public async Task<IEnumerable<LeagueTeam>?> GetTeamsAsync(League league)
    {
        if (!Supports.Contains(league.SiteId) || league.Info is not LeagueGamingLeagueInfo)
            return null;

        var leagueInfo = (league.Info as LeagueGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(
            parameters: new Dictionary<string, object?>
            {
                ["action"] = "league",
                ["page"] = "standing",
                ["leagueid"] = leagueInfo.LeagueId,
                ["seasonid"] = leagueInfo.SeasonId,
            }));

        var nameMatches = Regex.Matches(html,
            @$"<div[^>]*\bteam_box_icon\b[^>]*>.*?<a[^>]*page=team_page&(?:amp;)?teamid=(\d+)&(?:amp;)?leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid={leagueInfo.SeasonId}[^>]*>(.*?)</a>\s*</div>",
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