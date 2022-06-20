using System.Text.RegularExpressions;
using Duthie.Types.Modules.Api;
using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Modules.TheSpnhl;

public class TheSpnhlApi
    : IGameApi, ILeagueApi, ITeamApi
{
    private const string Domain = "thespnhl.com";
    private static readonly TimeZoneInfo Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private readonly HttpClient _httpClient = new HttpClient();

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { TheSpnhlSiteProvider.SPNHL.Id };
    }

    private bool IsSupported(League league) =>
        Supports.Contains(league.SiteId) || league.Info is TheSpnhlLeagueInfo;

    public async Task<IEnumerable<Game>?> GetGamesAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as TheSpnhlLeagueInfo)!;
            var html = await _httpClient.GetStringAsync($"https://{Domain}/calendar/fixtures-results/");

            return Regex.Matches(html,
                @"<span[^>]*\bteam-logo\b[^>]*>\s*<meta(?=[^>]*itemprop=""name"")[^>]*content=""(.*?)""[^>]*>\s*<a[^>]*>\s*<img[^>]*>\s*</a>\s*</span>\s*<span[^>]*\bteam-logo\b[^>]*>\s*<meta(?=[^>]*itemprop=""name"")[^>]*content=""(.*?)""[^>]*>\s*<a[^>]*>\s*<img[^>]*>\s*</a>\s*</span>\s*<time(?=[^>]*\bsp-event-date\b)[^>]*content=""(.*?)""[^>]*>\s*<a[^>]*>.*?</a>\s*</time>\s*<h5[^>]*\bsp-event-results\b[^>]*>\s*<a(?=[^>]*itemprop=""url"")[^>]*/event/(\d+)[^>]*>\s*(?:<span[^>]*>([\dO]+)</span>\s*-\s*<span[^>]*>([\dO]+)</span>|<span[^>]*>.*?</span>)\s*</a>\s*</h5>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m => new Game
            {
                LeagueId = league.Id,
                Id = ulong.Parse(m.Groups[4].Value),
                Timestamp = DateTimeOffset.Parse(m.Groups[3].Value.Trim()),
                VisitorId = m.Groups[1].Value.Trim(),
                VisitorScore = m.Groups[5].Value.ToUpper() == "O"
                    ? 0
                    : int.TryParse(m.Groups[5].Value, out var visitorScore) ? visitorScore : null,
                HomeId = m.Groups[2].Value.Trim(),
                HomeScore = m.Groups[6].Value.ToUpper() == "O"
                    ? 0
                    : int.TryParse(m.Groups[6].Value, out var homeScore) ? homeScore : null,
            })
            .ToList();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching games for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<Types.Modules.Data.League?> GetLeagueAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as TheSpnhlLeagueInfo)!;
            var html = await _httpClient.GetStringAsync($"https://{Domain}/calendar/fixtures-results/");

            var logo = Regex.Match(html,
                @"<a[^>]*\bsite-logo\b[^>]*>\s*<img[^>]*src=([""'])(.*?)\1[^>]*>\s*</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var season = Regex.Match(html,
                @"Season\s*(\d+)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return new Types.Modules.Data.League
            {
                Id = league.Id,
                Name = league.Name,
                LogoUrl = logo.Success ? $"https://{Domain}/{Regex.Replace(logo.Groups[2].Value.Trim(), @$"^(https://{Domain})?/?", "")}" : league.LogoUrl,
                Info = new TheSpnhlLeagueInfo
                {
                    LeagueType = leagueInfo.LeagueType,
                    SeasonId = season.Success ? int.Parse(season.Groups[1].Value) : leagueInfo.SeasonId
                },
            };
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching info for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<Team>?> GetTeamsAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as TheSpnhlLeagueInfo)!;
            var html = await _httpClient.GetStringAsync($"https://{Domain}/standings/");

            var matches = Regex.Matches(html,
                @"<a[^>]*><span[^>]*\bteam-logo\b[^>]*>\s*<img[^>]*>\s*</span>(.*?)</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (matches.Count() == 0)
                return null;

            return matches
                .Cast<Match>()
                .DistinctBy(m => m.Groups[1].Value.Trim())
                .ToDictionary(
                    m => m.Groups[1].Value.Trim(),
                    m =>
                    {
                        var team = Types.Teams.DefaultTeams.GetByAbbreviation(m.Groups[1].Value.Trim(), leagueInfo.LeagueType);

                        if (team == null)
                            return null;

                        return new Team
                        {
                            LeagueId = league.Id,
                            Id = m.Groups[1].Value.Trim(),
                            Name = team.Name,
                            ShortName = team.ShortName,
                        };
                    },
                    StringComparer.OrdinalIgnoreCase)
                .Values
                .Where(t => t != null)
                .Cast<Team>()
                .ToList();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching teams for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public string? GetGameUrl(League league, Game game)
    {
        if (!IsSupported(league))
            return null;

        return $"https://{Domain}/event/{game.Id}/";
    }
}