using System.Text.RegularExpressions;
using System.Web;
using Duthie.Types.Api;
using Duthie.Types.Games;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;

namespace Duthie.Modules.TheSpnhl;

public class TheSpnhlApi : IGamesApi, ILeagueInfoApi, ITeamsApi
{
    private static readonly TimeZoneInfo Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private readonly HttpClient _httpClient = new HttpClient();

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { TheSpnhlSiteProvider.SITE_ID };
    }

    public async Task<IEnumerable<ApiGame>?> GetGamesAsync(League league)
    {
        if (!Supports.Contains(league.SiteId) || league.Info is not TheSpnhlLeagueInfo)
            return null;

        var leagueInfo = (league.Info as TheSpnhlLeagueInfo)!;
        var html = await _httpClient.GetStringAsync("https://thespnhl.com/calendar/fixtures-results/");

        return Regex.Matches(html,
            @"<span[^>]*\bteam-logo\b[^>]*>\s*<meta(?=[^>]*itemprop=""name"")[^>]*content=""(.*?)""[^>]*>\s*<a[^>]*>\s*<img[^>]*>\s*</a>\s*</span>\s*<span[^>]*\bteam-logo\b[^>]*>\s*<meta(?=[^>]*itemprop=""name"")[^>]*content=""(.*?)""[^>]*>\s*<a[^>]*>\s*<img[^>]*>\s*</a>\s*</span>\s*<time(?=[^>]*\bsp-event-date\b)[^>]*content=""(.*?)""[^>]*>\s*<a[^>]*>.*?</a>\s*</time>\s*<h5[^>]*\bsp-event-results\b[^>]*>\s*<a(?=[^>]*itemprop=""url"")[^>]*/event/(\d+)[^>]*>\s*(?:<span[^>]*>([\dO]+)</span>\s*-\s*<span[^>]*>([\dO]+)</span>|<span[^>]*>.*?</span>)\s*</a>\s*</h5>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .Select(m => new ApiGame
        {
            LeagueId = league.Id,
            GameId = m.Groups[4].Value.Trim(),
            Date = DateTimeOffset.Parse(m.Groups[3].Value.Trim()),
            VisitorIId = m.Groups[1].Value.Trim(),
            VisitorScore = m.Groups[5].Value.ToUpper() == "O"
                ? 0
                : int.TryParse(m.Groups[5].Value, out var visitorScore) ? visitorScore : null,
            HomeIId = m.Groups[2].Value.Trim(),
            HomeScore = m.Groups[6].Value.ToUpper() == "O"
                ? 0
                : int.TryParse(m.Groups[6].Value, out var homeScore) ? homeScore : null,
        });
    }

    public async Task<ILeague?> GetLeagueInfoAsync(League league)
    {
        if (!Supports.Contains(league.SiteId) || league.Info is not TheSpnhlLeagueInfo)
            return null;

        var leagueInfo = (league.Info as TheSpnhlLeagueInfo)!;
        var html = await _httpClient.GetStringAsync("https://thespnhl.com/calendar/fixtures-results/");

        var season = Regex.Match(html,
            @$"Season\s*(\d+)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!season.Success)
            return null;

        return new League
        {
            Name = league.Name,
            Info = new TheSpnhlLeagueInfo
            {
                LeagueType = leagueInfo.LeagueType,
                SeasonId = season.Success ? int.Parse(season.Groups[1].Value) : leagueInfo.SeasonId
            }
        };
    }

    public async Task<IEnumerable<LeagueTeam>?> GetTeamsAsync(League league)
    {
        if (!Supports.Contains(league.SiteId) || league.Info is not TheSpnhlLeagueInfo)
            return null;

        var leagueInfo = (league.Info as TheSpnhlLeagueInfo)!;
        var html = await _httpClient.GetStringAsync("https://thespnhl.com/standings/");

        var matches = Regex.Matches(html,
            @$"<a[^>]*><span[^>]*\bteam-logo\b[^>]*>\s*<img[^>]*>\s*</span>(.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (matches.Count() == 0)
            return null;

        return matches
            .DistinctBy(m => m.Groups[1].Value)
            .ToDictionary(
                m => m.Groups[1].Value,
                m =>
                {
                    var team = DefaultTeams.GetByAbbreviation(m.Groups[1].Value.Trim(), leagueInfo.LeagueType);

                    if (team == null)
                        return null;

                    return new LeagueTeam
                    {
                        LeagueId = league.Id,
                        League = league,
                        TeamId = team.Id,
                        Team = team,
                        IId = m.Groups[1].Value.Trim()
                    };
                })
            .Values
            .Where(t => t != null)
            .Cast<LeagueTeam>()
            .ToList();
    }
}