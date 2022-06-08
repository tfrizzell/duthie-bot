using System.Text.RegularExpressions;
using System.Web;
using Duthie.Types.Modules.Api;
using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Modules.LeagueGaming;

public class LeagueGamingApi
    : IBidApi, IContractApi, IGameApi, ILeagueApi, ITeamApi, ITradeApi
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
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = leagueInfo.SeasonId,
                    ["typeid"] = (int)LeagueGamingNewsType.Bids,
                    ["displaylimit"] = 200,
                }));

            return Regex.Matches(html,
                @"<li[^>]*\bNewsFeedItem\b[^>]*>(.*?)</li>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var bid = Regex.Match(m.Groups[1].Value,
                    @"<h3[^>]*>\s*<img[^>]*team(\d+)\.\w{3,4}[^>]*>\s*<span[^>]*\bnewsfeed_atn2\b[^>]*>(.*?)</span>\s*have earned the player rights for\s*<span[^>]*\bnewsfeed_atn\b[^>]*>(.*?)</span>\s*with a bid amount of\s*<span[^>]*\bnewsfeed_atn2\b[^>]*>(\$[\d,]+)</span>.*?</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!bid.Success)
                    return null;

                var dateTime = DateTime.Parse(bid.Groups[5].Value.Trim());

                return new Bid
                {
                    LeagueId = league.Id,
                    TeamId = bid.Groups[1].Value.Trim(),
                    PlayerName = bid.Groups[3].Value.Trim(),
                    Amount = ISiteApi.ParseDollars(bid.Groups[4].Value),
                    State = BidState.Won,
                    Timestamp = new DateTimeOffset(dateTime, Timezone.GetUtcOffset(dateTime)),
                };
            })
            .Where(b => b != null)
            .Cast<Bid>();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching bids for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<Contract>?> GetContractsAsync(League league)
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
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = leagueInfo.SeasonId,
                    ["typeid"] = (int)LeagueGamingNewsType.Contracts,
                    ["displaylimit"] = 200,
                }));

            return Regex.Matches(html,
                @"<li[^>]*\bNewsFeedItem\b[^>]*>(.*?)</li>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var contract = Regex.Match(m.Groups[1].Value,
                    @"<h3[^>]*>\s*<span[^>]*\bnewsfeed_atn\b[^>]*>(.*?)</span>\s*and the\s*<img[^>]*/team(\d+).\w{3,4}[^>]*>\s*<span[^>]*\bnewsfeed_atn2\b[^>]*>(.*?)</span>\s*have agreed to a (\d+) season deal at (\$[\d,]+) per season</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!contract.Success)
                    return null;

                var dateTime = DateTime.Parse(contract.Groups[6].Value.Trim());

                return new Contract
                {
                    LeagueId = league.Id,
                    TeamId = contract.Groups[2].Value.Trim(),
                    PlayerName = contract.Groups[1].Value.Trim(),
                    Length = int.TryParse(contract.Groups[4].Value.Trim(), out var length) ? length : 1,
                    Amount = ISiteApi.ParseDollars(contract.Groups[5].Value),
                    Timestamp = new DateTimeOffset(dateTime, Timezone.GetUtcOffset(dateTime)),
                };
            })
            .Where(c => c != null)
            .Cast<Contract>();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching contracts for league \"{league.Name}\" [{league.Id}]", e);
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
                    Id = ulong.Parse(m.Groups[5].Value.Trim()),
                    Timestamp = date.GetValueOrDefault(),
                    VisitorId = m.Groups[4].Value.Trim(),
                    VisitorScore = int.TryParse(m.Groups[8].Value, out var visitorScore) ? visitorScore : null,
                    HomeId = m.Groups[11].Value.Trim(),
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

    public async Task<Types.Modules.Data.League?> GetLeagueAsync(League league)
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
                @$"<a[^>]*leagueid={leagueInfo.LeagueId}&seasonid=(\d+)[^>]*>Roster</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return new Types.Modules.Data.League
            {
                Id = league.Id,
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

    public async Task<IEnumerable<Team>?> GetTeamsAsync(League league)
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
                @$"<div[^>]*\bteam_box_icon\b[^>]*>.*?<a[^>]*page=team_page&teamid=(\d+)&leagueid={leagueInfo.LeagueId}&seasonid={leagueInfo.SeasonId}[^>]*>(.*?)</a>\s*</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (nameMatches.Count() == 0)
                return null;

            var teams = nameMatches
                .Cast<Match>()
                .DistinctBy(m => m.Groups[1].Value)
                .ToDictionary(
                    m => m.Groups[1].Value,
                    m => new Team
                    {
                        LeagueId = league.Id,
                        Id = m.Groups[1].Value,
                        Name = m.Groups[2].Value.Trim(),
                        ShortName = m.Groups[2].Value.Trim(),
                    },
                    StringComparer.OrdinalIgnoreCase);

            var shortNameMatches = Regex.Matches(html,
                @$"<td[^>]*>\s*<img[^>]*/team\d+\.\w{{3,4}}[^>]*>\s*\d+\)\s*.*?\*?<a[^>]*page=team_page&teamid=(\d+)&leagueid=(?:{leagueInfo.LeagueId})?&seasonid=(?:{leagueInfo.SeasonId})?[^>]*>(.*?)</a>\s*</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>();

            foreach (var match in shortNameMatches)
            {
                var id = match.Groups[1].Value;

                if (!teams.ContainsKey(id))
                    teams.Add(id, new Team { LeagueId = league.Id, Id = id });

                teams[id].ShortName = match.Groups[2].Value.Trim();

                if (string.IsNullOrWhiteSpace(teams[id].Name))
                    teams[id].Name = teams[id].ShortName;
            }

            return teams.Values;
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching teams for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<Trade>?> GetTradesAsync(League league)
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
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = leagueInfo.SeasonId,
                    ["typeid"] = (int)LeagueGamingNewsType.Trades,
                    ["displaylimit"] = 200,
                }));

            return Regex.Matches(html,
                @"<li[^>]*\bNewsFeedItem\b[^>]*>(.*?)</li>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var trade = Regex.Match(m.Groups[1].Value,
                    @"<h3[^>]*>.*?<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*have traded\s*(.*?)\s*to the\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*for\s*(.*?)\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!trade.Success)
                    return null;

                var dateTime = DateTime.Parse(trade.Groups[5].Value.Trim());

                return new Trade
                {
                    LeagueId = league.Id,
                    FromId = trade.Groups[1].Value.Trim(),
                    ToId = trade.Groups[3].Value.Trim(),
                    FromAssets = Regex.Split(Regex.Replace(trade.Groups[2].Value, @"<[^>]*>", ""), @"\s*&\s*").Select(a => a.Trim()).Where(a => a.ToLower() != "nothing").ToArray(),
                    ToAssets = Regex.Split(Regex.Replace(trade.Groups[4].Value, @"<[^>]*>", ""), @"\s*&\s*").Select(a => a.Trim()).Where(a => a.ToLower() != "nothing").ToArray(),
                    Timestamp = new DateTimeOffset(dateTime, Timezone.GetUtcOffset(dateTime)),
                };
            })
            .Where(b => b != null)
            .Cast<Trade>();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching bids for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public string? GetBidUrl(League league, Bid bid)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as LeagueGamingLeagueInfo)!;
        return $"{Host}/forums/index.php?leaguegaming/league&action=league&page=team_news&leagueid={leagueInfo.LeagueId}&seasonid={leagueInfo.SeasonId}&teamid={bid.TeamId}&typeid={(int)LeagueGamingNewsType.Bids}";
    }

    public string? GetContractUrl(League league, Contract contract)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as LeagueGamingLeagueInfo)!;
        return $"{Host}/forums/index.php?leaguegaming/league&action=league&page=team_news&leagueid={leagueInfo.LeagueId}&seasonid={leagueInfo.SeasonId}&teamid={contract.TeamId}&typeid={(int)LeagueGamingNewsType.Contracts}";
    }

    public string? GetGameUrl(League league, Game game)
    {
        if (!IsSupported(league))
            return null;

        return $"{Host}/forums/index.php?leaguegaming/league&action=league&page=game&gameid={game.Id}";
    }

    public string? GetTradeUrl(League league, Trade trade)
    {
        if (!IsSupported(league))
            return null;

        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as LeagueGamingLeagueInfo)!;
        return $"{Host}/forums/index.php?leaguegaming/league&action=league&page=team_news&leagueid={leagueInfo.LeagueId}&seasonid={leagueInfo.SeasonId}&teamid={trade.FromId}&typeid={(int)LeagueGamingNewsType.Trades}";
    }
}