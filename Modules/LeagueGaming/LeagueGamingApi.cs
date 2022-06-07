using System.Text.RegularExpressions;
using System.Web;
using Duthie.Types.Api;
using Duthie.Types.Api.Data;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;

namespace Duthie.Modules.LeagueGaming;

public class LeagueGamingApi
    : IBidApi, IGameApi, ILeagueApi, ITeamApi
{
    private const string Host = "https://www.leaguegaming.com";
    private static readonly TimeZoneInfo Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private readonly HttpClient _httpClient = new HttpClient();

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { LeagueGamingSiteProvider.Leaguegaming.Id };
    }

    private string GetUrl(string file = "index.php", string path = "leaguegaming/league", IDictionary<string, object?>? parameters = null)
    {
        var queryString = parameters == null ? string.Empty
            : string.Join("&", parameters.Where(p => p.Value != null).Select(p => string.Join("=", HttpUtility.UrlEncode(p.Key), HttpUtility.UrlEncode(p.Value!.ToString()))));
        return Regex.Replace($"{Host}/forums/{file}?{path}&{queryString}".Replace("?&", "?"), @"[?&]+$", "");
    }

    private bool IsSupported(League league) =>
        Supports.Contains(league.SiteId) || league.Info is LeagueGamingLeagueInfo;

    public async Task<IEnumerable<Bid>?> GetBidsAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as LeagueGamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(
                parameters: new Dictionary<string, object?>
                {
                    ["action"] = "league",
                    ["page"] = "team_news",
                    ["teamid"] = 0,
                    ["typeid"] = LeagueGamingNewsType.Bid,
                    ["displaylimit"] = 200,
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = leagueInfo.SeasonId,
                }));

            return Regex.Matches(html,
                @"<h3[^>]*>\s*<img[^>]*team(\d+)\.\w{3,4}[^>]*>\s*<span[^>]*\bnewsfeed_atn2\b[^>]*>(.*?)</span>\s*have earned the player rights for\s*<span[^>]*\bnewsfeed_atn\b[^>]*>(.*?)</span>\s*with a bid amount of\s*<span[^>]*\bnewsfeed_atn2\b[^>]*>(\$[\d,]+)</span>.*?</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var dateTime = DateTime.Parse(m.Groups[5].Value.Trim());

                return new Bid
                {
                    LeagueId = league.Id,
                    TeamExternalId = m.Groups[1].Value.Trim(),
                    PlayerName = m.Groups[3].Value.Trim(),
                    Amount = ISiteApi.ParseDollars(m.Groups[4].Value),
                    State = BidState.Won,
                    Timestamp = new DateTimeOffset(dateTime, Timezone.GetUtcOffset(dateTime)),
                };
            });
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching bids for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<Game>?> GetGamesAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
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

            DateTimeOffset? date = null;

            return Regex.Matches(html,
                @$"(?:{string.Join("|",
                    @"<h4[^>]*sh4[^>]*>(.*?)</h4>",
                    @"<span[^>]*sweekid[^>]*>Week\s*(\d+)</span>\s*(?:<span[^>]*sgamenumber[^>]*>Game\s*#\s*(\d+)</span>)?\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*<a[^>]*&gameid=(\d+)[^>]*>\s*<span[^>]*steamname[^>]*>(.*?)</span>\s*<span[^>]*sscore[^>]*>(vs|(\d+)\D+(\d+))</span>\s*<span[^>]*steamname[^>]*>(.*?)</span>\s*</a>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>")})",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                if (!string.IsNullOrWhiteSpace(m.Groups[1].Value))
                {
                    date = ISiteApi.ParseDateWithNoYear(Regex.Replace(m.Groups[1].Value, @"(\d+)[\D\S]{2}", @"$1"));
                    return null;
                }

                if (date == null)
                    return null;

                return new Game
                {
                    LeagueId = league.Id,
                    GameId = ulong.Parse(m.Groups[5].Value.Trim()),
                    Timestamp = date.GetValueOrDefault(),
                    VisitorExternalId = m.Groups[4].Value.Trim(),
                    VisitorScore = int.TryParse(m.Groups[8].Value, out var visitorScore) ? visitorScore : null,
                    HomeExternalId = m.Groups[11].Value.Trim(),
                    HomeScore = int.TryParse(m.Groups[9].Value, out var homeScore) ? homeScore : null,
                };
            })
            .Where(g => g != null)
            .Cast<Game>();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching games for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<ILeague?> GetLeagueAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
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
                @$"<li[^>]*\bcustom-tab-{leagueInfo.LeagueId}\b[^>]*>\s*<a[^>]*forums/[^>]*\.(\d+)[^>]*>.*?<span[^>]*>(.*?)</span>.*?</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!info.Success)
                return null;

            var season = Regex.Match(html,
                @$"<a[^>]*leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid=(\d+)[^>]*>Roster</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return new League
            {
                Name = info.Groups[2].Value.Trim(),
                LogoUrl = $"{Host}/images/league/icon/l{leagueInfo.LeagueId}_100.png",
                Info = new LeagueGamingLeagueInfo
                {
                    LeagueId = leagueInfo.LeagueId,
                    SeasonId = season.Success ? int.Parse(season.Groups[1].Value) : leagueInfo.SeasonId,
                    ForumId = int.Parse(info.Groups[1].Value),
                }
            };
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching info for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<LeagueTeam>?> GetTeamsAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
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

            var teams = nameMatches
                .Cast<Match>()
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
                        ExternalId = m.Groups[1].Value,
                    },
                    StringComparer.OrdinalIgnoreCase);

            var shortNameMatches = Regex.Matches(html,
                @$"<td[^>]*><img[^>]*/team\d+\.\w{3,4}[^>]*> \d+\) .*?\*?<a[^>]*page=team_page&(?:amp;)?teamid=(\d+)&(?:amp;)?leagueid=(?:{leagueInfo.LeagueId})?&(?:amp;)?seasonid=(?:{leagueInfo.SeasonId})?[^>]*>(.*?)</a>.*?</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>();

            foreach (var match in shortNameMatches)
            {
                var id = match.Groups[1].Value;

                if (!teams.ContainsKey(id))
                    teams.Add(id, new LeagueTeam { LeagueId = league.Id, League = league, Team = new Team() });

                teams[id].Team.ShortName = match.Groups[2].Value.Trim();
                teams[id].ExternalId = id;

                if (string.IsNullOrWhiteSpace(teams[id].Team.Name))
                    teams[id].Team.Name = teams[id].Team.ShortName;
            }

            return teams.Values.ToList();
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

        return $"{Host}/forums/index.php?leaguegaming/league&action=league&page=game&gameid={game.GameId}";
    }
}