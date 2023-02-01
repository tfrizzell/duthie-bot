using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Duthie.Types.Modules.Api;
using Duthie.Types.Modules.Data;
using Microsoft.Extensions.Caching.Memory;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Modules.Leaguegaming;

public class LeaguegamingApi
    : IBidApi, IContractApi, IDailyStarApi, IDraftApi, IGameApi, ILeagueApi, INewsApi, IRosterApi, ITeamApi, ITradeApi, IWaiverApi
{
    private const string Domain = "www.leaguegaming.com";
    private static readonly TimeZoneInfo Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private readonly HttpClient _httpClient = new HttpClient();
    private readonly IMemoryCache _memoryCache = new MemoryCache(new MemoryCacheOptions());

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { LeaguegamingSiteProvider.Leaguegaming.Id };
    }

    private string GetUrl(League league, string file = "index.php", string path = "leaguegaming/league", IDictionary<string, object?>? parameters = null)
    {
        var queryString = parameters == null ? string.Empty
            : string.Join("&", parameters.Where(p => p.Value != null).Select(p => string.Join("=", HttpUtility.UrlEncode(p.Key), HttpUtility.UrlEncode(p.Value!.ToString()))));
        return Regex.Replace($"https://{Domain}/forums/{file}?{path}&{queryString}".Replace("?&", "?"), @"[?&]+$", "");
    }

    private bool IsSupported(League league) =>
        Supports.Contains(league.SiteId) || league.Info is LeaguegamingLeagueInfo;

    public async Task<IEnumerable<Bid>?> GetBidsAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                parameters: new Dictionary<string, object?>
                {
                    ["action"] = "league",
                    ["page"] = "team_news",
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = leagueInfo.SeasonId,
                    ["typeid"] = (int)LeaguegamingNewsType.Bids,
                    ["displaylimit"] = 200,
                }));

            return Regex.Matches(html,
                @"<li[^>]*\bNewsFeedItem\b[^>]*>(?<item>.*?)</li>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var bid = Regex.Match(m.Groups["item"].Value,
                    @"<h3[^>]*>\s*<img[^>]*team(?<teamId>\d+)\.\w{3,4}[^>]*>\s*<span[^>]*\bnewsfeed_atn2\b[^>]*>(?<teamName>.*?)</span>\s*have earned the player rights for\s*<span[^>]*\bnewsfeed_atn\b[^>]*>(?<playerName>.*?)</span>\s*with a bid amount of\s*<span[^>]*\bnewsfeed_atn2\b[^>]*>(?<amount>\$[\d,]+)</span>.*?</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!bid.Success)
                    return null;

                return new Bid
                {
                    LeagueId = league.Id,
                    TeamId = bid.Groups["teamId"].Value,
                    PlayerName = bid.Groups["playerName"].Value.Trim(),
                    Amount = ISiteApi.ParseDollars(bid.Groups["amount"].Value),
                    State = BidState.Won,
                    Timestamp = ISiteApi.ParseDateTime(bid.Groups["timestamp"].Value, Timezone),
                };
            })
            .Where(b => b != null)
            .Cast<Bid>()
            .ToList();
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

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                parameters: new Dictionary<string, object?>
                {
                    ["action"] = "league",
                    ["page"] = "team_news",
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = leagueInfo.SeasonId,
                    ["typeid"] = (int)LeaguegamingNewsType.Contracts,
                    ["displaylimit"] = 200,
                }));

            return Regex.Matches(html,
                @"<li[^>]*\bNewsFeedItem\b[^>]*>(?<item>.*?)</li>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var contract = Regex.Match(m.Groups["item"].Value,
                    @"<h3[^>]*>\s*<span[^>]*\bnewsfeed_atn\b[^>]*>(?<playerName>.*?)</span>\s*and the\s*<img[^>]*/team(?<teamId>\d+).\w{3,4}[^>]*>\s*<span[^>]*\bnewsfeed_atn2\b[^>]*>(?<teamName>.*?)</span>\s*have agreed to a (?<length>\d+) season deal at (?<amount>\$[\d,]+) per season</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!contract.Success)
                    return null;

                return new Contract
                {
                    LeagueId = league.Id,
                    TeamId = contract.Groups["teamId"].Value,
                    PlayerName = contract.Groups["playerName"].Value.Trim(),
                    Length = int.TryParse(contract.Groups["length"].Value, out var length) ? length : 1,
                    Amount = ISiteApi.ParseDollars(contract.Groups["amount"].Value),
                    Timestamp = ISiteApi.ParseDateTime(contract.Groups["timestamp"].Value, Timezone),
                };
            })
            .Where(c => c != null)
            .Cast<Contract>()
            .ToList();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching contracts for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<DailyStar>?> GetDailyStarsAsync(League league, DateTimeOffset? timestamp = null)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;
            var date = (timestamp ?? DateTimeOffset.UtcNow.ToOffset(Timezone.BaseUtcOffset).AddDays(-1).Date);
            var url = await GetDailyStarsUrl(league, date);

            if (string.IsNullOrWhiteSpace(url))
                return new List<DailyStar>();

            var html = await _httpClient.GetStringAsync(url);
            string position = "";

            return Regex.Matches(html,
                @$"(?:{string.Join("|",
                    @"<div[^>]*\bd3_title\b[^>]*>(?<position>.*?)</div>",
                    @"<tr[^>]*>\s*<td[^>]*>\s*(?<rank>(?:<img[^>]*/star\.\w{3,4}[^>]*>)+|\d+\.)\s*</td>\s*(?:<td[^>]*t_threestars[^>]*>\s*<div[^>]*>\s*<img[^>]*/team(?<teamId>\d+)\.\w{3,4}[^>]*>\s*<img[^>]*>\s*</div>\s*</td>\s*<td[^>]*>.*?(?:\s*<span[^>]*>\s*\d+\s*</span>\s*)?(?<playerName>.*?)\s*<br[^>]*>\s*<span[^>]*>\((.*?)\)</span>\s*</td>|<td[^>]*>\s*<img[^>]*/team(?<teamId>\d+)\.\w{3,4}[^>]*>.*?(?:\s*<span[^>]*>\s*\d+\s*</span>\s*)?(?<playerName>.*?)\s*\((.*?)\)</td>)\s*<td[^>]*>\s*<a[^>]*>.*?</a>\s*</td>\s*(?<stats>(?:<td[^>]*>.*?</td>)+)\s*</tr>"
                )})",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                if (!string.IsNullOrWhiteSpace(m.Groups["position"].Value))
                {
                    position = m.Groups["position"].Value.Trim();
                    return null;
                }

                if (string.IsNullOrWhiteSpace(position))
                    return null;

                var stats = Regex.Matches(m.Groups["stats"].Value,
                    @"<td[^>]*>(?<value>.*?)</td>");

                return position.Trim().ToUpper() switch
                {
                    "FORWARDS" => new DailyStarForward
                    {
                        LeagueId = league.Id,
                        TeamId = m.Groups["teamId"].Value.Trim(),
                        PlayerName = Regex.Replace(m.Groups["playerName"].Value, @"<(\S+)[^>]*>.*?</\1>", "").Trim(),
                        Rank = int.TryParse(m.Groups["rank"].Value.TrimEnd('.'), out var rank) ? rank : Regex.Matches(m.Groups["rank"].Value, @"star\.\w{3,4}").Count(),
                        Timestamp = date,
                        Goals = int.Parse(stats[1].Groups["value"].Value.Trim()),
                        Assists = int.Parse(stats[2].Groups["value"].Value.Trim()),
                        PlusMinus = int.Parse(stats[3].Groups["value"].Value.Trim()),
                    },

                    "DEFENDERS" => new DailyStarDefense
                    {
                        LeagueId = league.Id,
                        TeamId = m.Groups["teamId"].Value.Trim(),
                        PlayerName = Regex.Replace(m.Groups["playerName"].Value, @"<(\S+)[^>]*>.*?</\1>", "").Trim(),
                        Rank = int.TryParse(m.Groups["rank"].Value.TrimEnd('.'), out var rank) ? rank : Regex.Matches(m.Groups["rank"].Value, @"star\.\w{3,4}").Count(),
                        Timestamp = date,
                        Goals = int.Parse(stats[1].Groups["value"].Value.Trim()),
                        Assists = int.Parse(stats[2].Groups["value"].Value.Trim()),
                        PlusMinus = int.Parse(stats[3].Groups["value"].Value.Trim()),
                    },

                    "GOALIES" => new DailyStarGoalie
                    {
                        LeagueId = league.Id,
                        TeamId = m.Groups["teamId"].Value.Trim(),
                        PlayerName = Regex.Replace(m.Groups["playerName"].Value, @"<(\S+)[^>]*>.*?</\1>", "").Trim(),
                        Rank = int.TryParse(m.Groups["rank"].Value.TrimEnd('.'), out var rank) ? rank : Regex.Matches(m.Groups["rank"].Value, @"star\.\w{3,4}").Count(),
                        Timestamp = date,
                        GoalsAgainstAvg = decimal.Parse(stats[1].Groups["value"].Value.Trim()),
                        Saves = int.Parse(stats[2].Groups["value"].Value.Trim()),
                        ShotsAgainst = int.Parse(stats[3].Groups["value"].Value.Trim()),
                    },

                    _ => (DailyStar?)null,
                };
            })
            .Where(s => s != null)
            .Cast<DailyStar>()
            .ToList();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching daily stars for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    private async Task<string?> GetDailyStarsUrl(League league, DateTimeOffset? timestamp = null)
    {
        var date = (timestamp ?? DateTimeOffset.UtcNow.ToOffset(Timezone.BaseUtcOffset).AddDays(-1)).Date;

        return await _memoryCache.GetOrCreateAsync<string?>(new { type = GetType(), method = "GetAllAsync", league, date }, async entry =>
        {
            var titleText = $"Daily 3 Stars For {string.Format(date.ToString("dddd MMMM d{0}, yyyy"), GetSuffix(date.Day))}";
            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

            var xml = await _httpClient.GetStringAsync(GetUrl(league,
                path: $"forums/forum.{leagueInfo.ForumId}/index.rss")).ConfigureAwait(false);

            var doc = new XmlDocument();
            doc.LoadXml(xml);

            string? url = null;

            foreach (XmlNode item in doc.GetElementsByTagName("item"))
            {
                url = null;

                foreach (XmlNode node in item.ChildNodes)
                {
                    if (node.Name == "title" && node.InnerText.EndsWith(titleText, StringComparison.OrdinalIgnoreCase))
                    {
                        url = string.Empty;
                        break;
                    }
                }

                if (url == null)
                    continue;

                foreach (XmlNode node in item.ChildNodes)
                {
                    if (node.Name == "link")
                    {
                        url = node.InnerText;
                        break;
                    }
                }

                if (!string.IsNullOrWhiteSpace(url))
                    break;
            }

            entry.SetOptions(new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = url == null ? TimeSpan.FromSeconds(15) : TimeSpan.FromMinutes(15)
            });

            return url;
        }).ConfigureAwait(false);
    }

    private static string GetSuffix(int day)
    {
        var num = day.ToString();
        day %= 100;

        if ((day >= 11) && (day <= 13))
            return "th";

        switch (day % 10)
        {
            case 1: return "st";
            case 2: return "nd";
            case 3: return "rd";
            default: return "th";
        }
    }

    public async Task<IEnumerable<DraftPick>?> GetDraftPicksAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

            if (leagueInfo.DraftId == null || leagueInfo.DraftDate == null || leagueInfo.DraftDate > DateTimeOffset.UtcNow || leagueInfo.DraftDate < DateTimeOffset.UtcNow.AddDays(-1))
                return new List<DraftPick>();

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "leaguegaming/general",
                parameters: new Dictionary<string, object?>
                {
                    ["action"] = "general",
                    ["lggenm"] = "league_draft",
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["lgdraftid"] = leagueInfo.DraftId,
                }));

            var teamCount = Regex.Matches(html,
                @"<img[^>]*/team(?<teamId>\d+)\.\w{3,4}[^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .DistinctBy(m => m.Groups["teamId"].Value)
            .Count();

            var picks = Regex.Matches(html,
                @"<td[^>]*>(?<pickNumber>\d+)</td>\s*<td[^>]*>\s*<img[^>]*/team(?<teamId>\d+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>\s*<a[^>]*/member\.(?<playerId>\d+)[^>]*>(?<playerName>.*?)</a>\s*</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>();

            var rounds = (int)Math.Floor((decimal)picks.Count() / teamCount);
            var picksPerRound = (int)Math.Ceiling((decimal)picks.Count() / rounds);

            return picks.Select(m =>
            {
                var overallPick = int.Parse(m.Groups["pickNumber"].Value);
                var roundPick = overallPick % picksPerRound;

                return new DraftPick
                {
                    LeagueId = league.Id,
                    TeamId = m.Groups["teamId"].Value,
                    PlayerId = m.Groups["playerId"].Value,
                    PlayerName = m.Groups["playerName"].Value.Trim(),
                    RoundNumber = (int)Math.Ceiling((decimal)overallPick / picksPerRound),
                    RoundPick = roundPick > 0 ? roundPick : picksPerRound,
                    OverallPick = overallPick,
                };
            })
            .ToList();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching draft picks for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<Game>?> GetGamesAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
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
                    @"<h4[^>]*sh4[^>]*>(?<date>.*?)</h4>",
                    @"<span[^>]*sweekid[^>]*>\s*Week\s*(?<weekNumber>\d+)\s*</span>\s*(?:<span[^>]*sgamenumber[^>]*>\s*Game\s*#\s*(?<gameNumber>\d+)\s*</span>)?\s*<img[^>]*/team(?<visitorId>\d+)\.\w{3,4}[^>]*>\s*<a[^>]*&(?:amp;)?gameid=(?<gameId>\d+)[^>]*>\s*<span[^>]*steamname[^>]*>(?<visitorShortName>.*?)</span>\s*<span[^>]*sscore[^>]*>(?:vs|(?<visitorScore>\d+)\D+(?<homeScore>\d+))</span>\s*<span[^>]*steamname[^>]*>(?<homeShortName>.*?)</span>\s*</a>\s*<img[^>]*/team(?<homeId>\d+)\.\w{3,4}[^>]*>")})",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                if (!string.IsNullOrWhiteSpace(m.Groups["date"].Value))
                {
                    date = ISiteApi.ParseDateWithNoYear(Regex.Replace(m.Groups["date"].Value, @"(\d+)[\D\S]{2}", @"$1"), Timezone);
                    return null;
                }

                if (date == null)
                    return null;

                return new Game
                {
                    LeagueId = league.Id,
                    Id = ulong.Parse(m.Groups["gameId"].Value),
                    Timestamp = date.GetValueOrDefault(),
                    VisitorId = m.Groups["visitorId"].Value,
                    VisitorScore = int.TryParse(m.Groups["visitorScore"].Value, out var visitorScore) ? visitorScore : null,
                    HomeId = m.Groups["homeId"].Value,
                    HomeScore = int.TryParse(m.Groups["homeScore"].Value, out var homeScore) ? homeScore : null,
                };
            })
            .Where(g => g != null)
            .Cast<Game>()
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

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                parameters: new Dictionary<string, object?>
                {
                    ["action"] = "league",
                    ["page"] = "standing",
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = 1,
                }));

            var info = Regex.Match(html,
                @$"<li[^>]*\bcustom-tab-{leagueInfo.LeagueId}\b[^>]*>\s*<a[^>]*forums/[^>]*\.(?<forumId>\d+)[^>]*>.*?<span[^>]*>(?<leagueName>.*?)</span>.*?</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!info.Success)
                return null;

            var season = Regex.Match(html,
                @$"<a[^>]*leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid=(?<seasonId>\d+)[^>]*>Roster</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            html = await _httpClient.GetStringAsync(GetUrl(league,
                parameters: new Dictionary<string, object?>
                {
                    ["action"] = "league",
                    ["page"] = "drafts",
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = 1,
                }));

            var draft = Regex.Matches(html,
                @$"<td[^>]*>\s*<a[^>]*league_draft&(?:amp;)?leagueid={leagueInfo.LeagueId}&(?:amp;)?lgdraftid=(?<draftId>\d+)[^>]*>.*?</a>\s*</td>\s*<td[^>]*>(?<draftDate>.*?)</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .LastOrDefault();

            return new Types.Modules.Data.League
            {
                Id = league.Id,
                Name = info.Groups["leagueName"].Value.Trim(),
                LogoUrl = $"https://{Domain}/images/league/icon/l{leagueInfo.LeagueId}.png",
                Info = leagueInfo with
                {
                    SeasonId = season.Success ? int.Parse(season.Groups["seasonId"].Value) : leagueInfo.SeasonId,
                    ForumId = int.Parse(info.Groups["forumId"].Value),
                    DraftId = draft?.Success == true ? int.Parse(draft.Groups["draftId"].Value) : leagueInfo.DraftId,
                    DraftDate = draft?.Success == true ? ISiteApi.ParseDateTime(draft.Groups["draftDate"].Value) : leagueInfo.DraftDate,
                },
            };
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching info for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<News>?> GetNewsAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                parameters: new Dictionary<string, object?>
                {
                    ["action"] = "league",
                    ["page"] = "team_news",
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = leagueInfo.SeasonId,
                    ["typeid"] = (int)LeaguegamingNewsType.All,
                    ["displaylimit"] = 200,
                }));

            return Regex.Matches(html,
                @"<li[^>]*\bNewsFeedItem\b[^>]*>(?<item>.*?)</li>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var news = Regex.Match(m.Groups["item"].Value,
                    @"<a[^>]*\bicon\b[^>]*>\s*<img[^>]*/team(?<teamId>\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>(?=.*?(?:clinched|eliminated|rights have been acquired))(?<content>.*?)</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!news.Success)
                    return null;

                return new News
                {
                    LeagueId = league.Id,
                    TeamId = news.Groups["teamId"].Value,
                    Message = Regex.Replace(Regex.Replace(news.Groups["content"].Value, @"<[^>]*>", ""), @" +", " ").Trim(),
                    Timestamp = ISiteApi.ParseDateTime(news.Groups["timestamp"].Value, Timezone),
                };
            })
            .Where(n => n != null)
            .Cast<News>()
            .ToList();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching news for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<RosterTransaction>?> GetRosterTransactionsAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;
            var teamIds = await GetTeamIdsAsync(league);

            return (await Task.WhenAll(new LeaguegamingNewsType[] {
                LeaguegamingNewsType.AccountUpdate,
                LeaguegamingNewsType.Bans,
                LeaguegamingNewsType.CallUpDown,
                LeaguegamingNewsType.InjuredReserve,
                LeaguegamingNewsType.Suspensions,
            }
            .Distinct()
            .Select(async type =>
            {
                var html = await _httpClient.GetStringAsync(GetUrl(league,
                    parameters: new Dictionary<string, object?>
                    {
                        ["action"] = "league",
                        ["page"] = "team_news",
                        ["leagueid"] = leagueInfo.LeagueId,
                        ["seasonid"] = leagueInfo.SeasonId,
                        ["typeid"] = (int)type,
                        ["displaylimit"] = 200,
                    }));

                return Regex.Matches(html,
                    @"<li[^>]*\bNewsFeedItem\b[^>]*>(?<item>.*?)</li>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m =>
                {
                    var ban = Regex.Match(m.Groups["item"].Value,
                        @"<a[^>]*>\s*<img[^>]*/(?:team(?<teamId>\d+)|l\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*<span[^>]*>(?<playerName>.*?)</span>\s*has\s+been\s+issued\s+a\s*<span[^>]*>.*?Ban\s*</span>\s*in\s+Season\s+\d+\s+of\s+the\s*<span[^>]*>.*?</span>\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (ban.Success == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { ban.Groups["teamId"].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { ban.Groups["playerName"].Value.Trim() },
                            Type = RosterTransactionType.Banned,
                            Timestamp = ISiteApi.ParseDateTime(ban.Groups["timestamp"].Value, Timezone),
                        };
                    }

                    var callUp = Regex.Match(m.Groups["item"].Value,
                        @"<a[^>]*>\s*<img[^>]*/team(?<toTeamId>\d+)\.\w{3,4}[^>]*>.*?<a[^>]*>\s*<img[^>]*/arrow1\.\w{3,4}[^>]*>.*?<a[^>]*>\s*<img[^>]*/team(?<fromTeamId>\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*The\s*<img[^>]*/team\2\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*have\s+sent\s*<span[^>]*>(?<playerName>.*?)</span>\s*to\s+the\s*<img[^>]*/team\1\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (callUp.Success == true && teamIds.Contains(callUp.Groups["toTeamId"].Value) == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { callUp.Groups["toTeamId"].Value, callUp.Groups["fromTeamId"].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { callUp.Groups["playerName"].Value.Trim() },
                            Type = RosterTransactionType.CalledUp,
                            Timestamp = ISiteApi.ParseDateTime(callUp.Groups["timestamp"].Value, Timezone),
                        };
                    }

                    var callUpFromTc = Regex.Match(m.Groups["item"].Value,
                        @"<a[^>]*>\s*<img[^>]*/team(?<teamId>\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*<span[^>]*>(?<playerName>.*?)</span>\s*has\s+cleared\s*<span[^>]*>\s*Training\s+Camp\s*</span>\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (callUpFromTc.Success == true && teamIds.Contains(callUpFromTc.Groups["teamId"].Value) == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { callUpFromTc.Groups["teamId"].Value, Guid.Empty.ToString() }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { callUpFromTc.Groups["playerName"].Value.Trim() },
                            Type = RosterTransactionType.CalledUp,
                            Timestamp = ISiteApi.ParseDateTime(callUpFromTc.Groups["timestamp"].Value, Timezone),
                        };
                    }

                    var placedOnIr = Regex.Match(m.Groups["item"].Value,
                        @"<a[^>]*>\s*<img[^>]*/team(?<teamId>\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*<span[^>]*>(?<playerName>.*?)</span>\s*has\s+been\s+moved\s+to\s+the\s*<span[^>]*>\s*Injured\s+Reserve\s*</span>\s*list\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (placedOnIr.Success == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { placedOnIr.Groups["teamId"].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { placedOnIr.Groups["playerName"].Value.Trim() },
                            Type = RosterTransactionType.PlacedOnIr,
                            Timestamp = ISiteApi.ParseDateTime(placedOnIr.Groups["timestamp"].Value, Timezone),
                        };
                    }

                    var removedFromIr = Regex.Match(m.Groups["item"].Value,
                        @"<a[^>]*>\s*<img[^>]*/team(?<teamId>\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*<span[^>]*>(?<playerName>.*?)</span>\s*has\s+been\s+taken\s+off\s+the\s*<span[^>]*>\s*Injured\s+Reserve\s*</span>\s*list\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (removedFromIr.Success == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { removedFromIr.Groups["teamId"].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { removedFromIr.Groups["playerName"].Value.Trim() },
                            Type = RosterTransactionType.RemovedFromIr,
                            Timestamp = ISiteApi.ParseDateTime(removedFromIr.Groups["timestamp"].Value, Timezone),
                        };
                    }

                    var sendDown = Regex.Match(m.Groups["item"].Value,
                        @"<a[^>]*>\s*<img[^>]*/team(?<fromTeamId>\d+)\.\w{3,4}[^>]*>.*?<a[^>]*>\s*<img[^>]*/arrow2\.\w{3,4}[^>]*>.*?<a[^>]*>\s*<img[^>]*/team(?<toTeamId>\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*The\s*<img[^>]*/team\1\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*have\s+sent\s*<span[^>]*>(?<playerName>.*?)</span>\s*to\s+the\s*<img[^>]*/team\2\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (sendDown.Success == true && teamIds?.Contains(sendDown.Groups["fromTeamId"].Value) == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { sendDown.Groups["fromTeamId"].Value, sendDown.Groups["toTeamId"].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { sendDown.Groups["playerName"].Value.Trim() },
                            Type = RosterTransactionType.SentDown,
                            Timestamp = ISiteApi.ParseDateTime(sendDown.Groups["timestamp"].Value, Timezone),
                        };
                    }

                    var suspension = Regex.Match(m.Groups["item"].Value,
                        @"<a[^>]*>\s*<img[^>]*/(?:team(?<teamId>\d+)|l\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*<span[^>]*>(?<playerName>.*?)</span>\s*has\s+been\s+issued\s+a\s*<span[^>]*>.*?Suspension\s*</span>\s*in\s+Season\s+\d+\s+of\s+the\s*<span[^>]*>.*?</span>\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (suspension.Success == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { suspension.Groups["teamId"].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { suspension.Groups["playerName"].Value.Trim() },
                            Type = RosterTransactionType.Suspended,
                            Timestamp = ISiteApi.ParseDateTime(suspension.Groups["timestamp"].Value, Timezone),
                        };
                    }

                    return null;
                })
                .Where(t => t != null)
                .Cast<RosterTransaction>();
            })))
            .SelectMany(r => r)
            .Where(r => r != null)
            .ToList();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching contracts for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    private async Task<IEnumerable<string>> GetTeamIdsAsync(League league)
    {
        var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(league,
            parameters: new Dictionary<string, object?>
            {
                ["action"] = "league",
                ["page"] = "standing",
                ["leagueid"] = leagueInfo.LeagueId,
                ["seasonid"] = leagueInfo.SeasonId,
            }));

        return Regex.Matches(html,
            @$"<div[^>]*\bteam_box_icon\b[^>]*>.*?<a[^>]*page=team_page&(?:amp;)?teamid=(?<teamId>\d+)&(?:amp;)?leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid={leagueInfo.SeasonId}[^>]*>(.*?)</a>\s*</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .Cast<Match>()
        .Select(m => m.Groups["teamId"].Value);
    }

    public async Task<IEnumerable<Team>?> GetTeamsAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                parameters: new Dictionary<string, object?>
                {
                    ["action"] = "league",
                    ["page"] = "standing",
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = leagueInfo.SeasonId,
                }));

            var nameMatches = Regex.Matches(html,
                @$"<div[^>]*\bteam_box_icon\b[^>]*>.*?<a[^>]*page=team_page&(?:amp;)?teamid=(?<teamId>\d+)&(?:amp;)?leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid={leagueInfo.SeasonId}[^>]*>(?<teamName>.*?)</a>\s*</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (nameMatches.Count() == 0)
                return null;

            var teams = nameMatches
                .Cast<Match>()
                .DistinctBy(m => m.Groups["teamId"].Value)
                .ToDictionary(
                    m => m.Groups["teamId"].Value,
                    m => new Team
                    {
                        LeagueId = league.Id,
                        Id = m.Groups["teamId"].Value,
                        Name = m.Groups["teamName"].Value.Trim(),
                        ShortName = m.Groups["teamName"].Value.Trim(),
                    },
                    StringComparer.OrdinalIgnoreCase);

            var shortNameMatches = Regex.Matches(html,
                @$"<td[^>]*>\s*<img[^>]*/team\d+\.\w{{3,4}}[^>]*>\s*\d+\)\s*.*?\*?<a[^>]*page=team_page&(?:amp;)?teamid=(?<teamId>\d+)&(?:amp;)?leagueid=(?:{leagueInfo.LeagueId})?&(?:amp;)?seasonid=(?:{leagueInfo.SeasonId})?[^>]*>(?<teamShortName>.*?)</a>\s*</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>();

            foreach (var match in shortNameMatches)
            {
                var id = match.Groups["teamId"].Value;

                if (!teams.ContainsKey(id))
                    teams.Add(id, new Team { LeagueId = league.Id, Id = id });

                teams[id].ShortName = match.Groups["teamShortName"].Value.Trim();

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

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                parameters: new Dictionary<string, object?>
                {
                    ["action"] = "league",
                    ["page"] = "team_news",
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = leagueInfo.SeasonId,
                    ["typeid"] = (int)LeaguegamingNewsType.Trades,
                    ["displaylimit"] = 200,
                }));

            return Regex.Matches(html,
                @"<li[^>]*\bNewsFeedItem\b[^>]*>(?<item>.*?)</li>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var trade = Regex.Match(m.Groups["item"].Value,
                    @"<h3[^>]*>.*?<img[^>]*/team(?<fromTeamId>\d+)\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*have traded\s*(?<fromAssets>.*?)\s*to the\s*<img[^>]*/team(?<toTeamId>\d+)\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*for\s*(?<toAssets>.*?)\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!trade.Success)
                    return null;

                return new Trade
                {
                    LeagueId = league.Id,
                    FromId = trade.Groups["fromTeamId"].Value,
                    ToId = trade.Groups["toTeamId"].Value,
                    FromAssets = Regex.Split(Regex.Replace(trade.Groups["fromAssets"].Value, @"<[^>]*>", ""), @"\s*&\s*").Select(a => a.Trim()).Where(a => a.ToLower() != "nothing").ToArray(),
                    ToAssets = Regex.Split(Regex.Replace(trade.Groups["toAssets"].Value, @"<[^>]*>", ""), @"\s*&\s*").Select(a => a.Trim()).Where(a => a.ToLower() != "nothing").ToArray(),
                    Timestamp = ISiteApi.ParseDateTime(trade.Groups["timestamp"].Value, Timezone),
                };
            })
            .Where(b => b != null)
            .Cast<Trade>()
            .ToList();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching bids for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<Waiver>?> GetWaiversAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                parameters: new Dictionary<string, object?>
                {
                    ["action"] = "league",
                    ["page"] = "team_news",
                    ["leagueid"] = leagueInfo.LeagueId,
                    ["seasonid"] = leagueInfo.SeasonId,
                    ["typeid"] = (int)LeaguegamingNewsType.Waivers,
                    ["displaylimit"] = 200,
                }));

            return Regex.Matches(html,
                @"<li[^>]*\bNewsFeedItem\b[^>]*>(?<item>.*?)</li>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var waiver = Regex.Match(m.Groups["item"].Value,
                    @$"<h3[^>]*>.*?(?:{string.Join("|",
                        @"<img[^>]*/team(?<teamId>\d+)\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*have\s*(?<action>placed|removed|claimed)\s*<span[^>]*>(?<playerName>.*?)</span>",
                        @"<span[^>]*>(?<playerName>.*?)</span>\s*has\s+cleared\s+waivers\s+and\s+is\s+reporting\s+to.*?\s*<img[^>]*/team(?<teamId>\d+)\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>"
                    )}).*?</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(?<timestamp>.*?)</abbr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!waiver.Success)
                    return null;

                return new Waiver
                {
                    LeagueId = league.Id,
                    TeamId = waiver.Groups["teamId"].Value.Trim(),
                    PlayerName = waiver.Groups["playerName"].Value.Trim(),
                    Type = Enum.TryParse<WaiverActionType>(waiver.Groups["action"].Value, true, out var action) ? action : WaiverActionType.Cleared,
                    Timestamp = ISiteApi.ParseDateTime(waiver.Groups["timestamp"].Value, Timezone),
                };
            })
            .Where(w => w != null)
            .Cast<Waiver>()
            .ToList();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching waivers for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public string? GetBidUrl(League league, Bid bid)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;
        return $"https://{Domain}/forums/index.php?leaguegaming/league&action=league&page=team_news&leagueid={leagueInfo.LeagueId}&seasonid={leagueInfo.SeasonId}&teamid={bid.TeamId}&typeid={(int)LeaguegamingNewsType.Bids}";
    }

    public string? GetContractUrl(League league, Contract contract)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;
        return $"https://{Domain}/forums/index.php?leaguegaming/league&action=league&page=team_news&leagueid={leagueInfo.LeagueId}&seasonid={leagueInfo.SeasonId}&teamid={contract.TeamId}&typeid={(int)LeaguegamingNewsType.Contracts}";
    }

    public string? GetDailyStarsUrl(League league, DailyStar dailyStar)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;
        return GetDailyStarsUrl(league, dailyStar.Timestamp).Result ?? $"https://{Domain}/forums/index.php?forums/forum.{leagueInfo.ForumId}/";
    }

    public string? GetGameUrl(League league, Game game)
    {
        if (!IsSupported(league))
            return null;

        return $"https://{Domain}/forums/index.php?leaguegaming/league&action=league&page=game&gameid={game.Id}";
    }

    public string? GetNewsUrl(League league, News news)
    {
        if (!IsSupported(league))
            return null;

        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;
        return $"https://{Domain}/forums/index.php?leaguegaming/league&action=league&page=team_news&leagueid={leagueInfo.LeagueId}&seasonid={leagueInfo.SeasonId}&teamid={news.TeamId}&typeid={(int)LeaguegamingNewsType.All}";
    }

    public string? GetRosterTransactionUrl(League league, RosterTransaction rosterTransaction)
    {
        if (!IsSupported(league))
            return null;

        var type = rosterTransaction.Type switch
        {
            RosterTransactionType.PlacedOnIr or RosterTransactionType.RemovedFromIr => LeaguegamingNewsType.InjuredReserve,
            RosterTransactionType.CalledUp or RosterTransactionType.SentDown => LeaguegamingNewsType.CallUpDown,
            RosterTransactionType.Banned => LeaguegamingNewsType.Bans,
            RosterTransactionType.Suspended => LeaguegamingNewsType.Suspensions,
            _ => LeaguegamingNewsType.All,
        };

        var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;
        return $"https://{Domain}/forums/index.php?leaguegaming/league&action=league&page=team_news&leagueid={leagueInfo.LeagueId}&seasonid={leagueInfo.SeasonId}&teamid={rosterTransaction.TeamIds.FirstOrDefault() ?? ""}&typeid={(int)type}";
    }

    public string? GetTradeUrl(League league, Trade trade)
    {
        if (!IsSupported(league))
            return null;

        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;
        return $"https://{Domain}/forums/index.php?leaguegaming/league&action=league&page=team_news&leagueid={leagueInfo.LeagueId}&seasonid={leagueInfo.SeasonId}&teamid={trade.FromId}&typeid={(int)LeaguegamingNewsType.Trades}";
    }

    public string? GetWaiverUrl(League league, Waiver waiver)
    {
        if (!IsSupported(league))
            return null;

        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as LeaguegamingLeagueInfo)!;
        return $"https://{Domain}/forums/index.php?leaguegaming/league&action=league&page=team_news&leagueid={leagueInfo.LeagueId}&seasonid={leagueInfo.SeasonId}&teamid={waiver.TeamId}&typeid={(int)LeaguegamingNewsType.Waivers}";
    }
}