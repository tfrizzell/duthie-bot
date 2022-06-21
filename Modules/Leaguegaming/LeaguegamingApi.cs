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

                return new Bid
                {
                    LeagueId = league.Id,
                    TeamId = bid.Groups[1].Value,
                    PlayerName = bid.Groups[3].Value.Trim(),
                    Amount = ISiteApi.ParseDollars(bid.Groups[4].Value),
                    State = BidState.Won,
                    Timestamp = ISiteApi.ParseDateTime(bid.Groups[5].Value, Timezone),
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

                return new Contract
                {
                    LeagueId = league.Id,
                    TeamId = contract.Groups[2].Value,
                    PlayerName = contract.Groups[1].Value.Trim(),
                    Length = int.TryParse(contract.Groups[4].Value, out var length) ? length : 1,
                    Amount = ISiteApi.ParseDollars(contract.Groups[5].Value),
                    Timestamp = ISiteApi.ParseDateTime(contract.Groups[6].Value, Timezone),
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
            string type = "";

            return Regex.Matches(html,
                @$"(?:{string.Join("|",
                    @"<div[^>]*\bd3_title\b[^>]*>(.*?)</div>",
                    @"<tr[^>]*>\s*<td[^>]*>\s*((?:<img[^>]*/star\.\w{3,4}[^>]*>)+|\d+\.)\s*</td>\s*(?:<td[^>]*t_threestars[^>]*>\s*<div[^>]*>\s*<img [^>]*/team(\d+)\.\w{3,4}[^>]*>\s*<img[^>]*>\s*</div>\s*</td>\s*<td[^>]*>.*?(?:\s*<span[^>]*>\s*\d+\s*</span>\s*)?(.*?)\s*<br[^>]*>\s*<span[^>]*>\((.*?)\)</span>\s*</td>|<td[^>]*>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>.*?(?:\s*<span[^>]*>\s*\d+\s*</span>\s*)?(.*?)\s*\((.*?)\)</td>)\s*<td[^>]*>\s*<a[^>]*>.*?</a>\s*</td>\s*((?:<td[^>]*>.*?</td>)+)\s*</tr>"
                )})",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                if (!string.IsNullOrWhiteSpace(m.Groups[1].Value))
                {
                    type = m.Groups[1].Value.Trim();
                    return null;
                }

                if (string.IsNullOrWhiteSpace(type))
                    return null;

                var data = Regex.Matches(m.Groups[9].Value,
                    @"<td[^>]*>(.*?)</td>");

                return type.Trim().ToUpper() switch
                {
                    "FORWARDS" => new DailyStarForward
                    {
                        LeagueId = league.Id,
                        TeamId = (string.IsNullOrWhiteSpace(m.Groups[3].Value) ? m.Groups[6].Value : m.Groups[3].Value).Trim(),
                        PlayerName = Regex.Replace((string.IsNullOrWhiteSpace(m.Groups[4].Value) ? m.Groups[7].Value : m.Groups[4].Value), @"<(\S+)[^>]*>.*?</\1>", "").Trim(),
                        Rank = int.TryParse(m.Groups[2].Value.TrimEnd('.'), out var rank) ? rank : Regex.Matches(m.Groups[2].Value, @"star\.\w{3,4}").Count(),
                        Timestamp = date,
                        Goals = int.Parse(data[1].Groups[1].Value.Trim()),
                        Assists = int.Parse(data[2].Groups[1].Value.Trim()),
                        PlusMinus = int.Parse(data[3].Groups[1].Value.Trim()),
                    },

                    "DEFENDERS" => new DailyStarDefense
                    {
                        LeagueId = league.Id,
                        TeamId = (string.IsNullOrWhiteSpace(m.Groups[3].Value) ? m.Groups[6].Value : m.Groups[3].Value).Trim(),
                        PlayerName = Regex.Replace((string.IsNullOrWhiteSpace(m.Groups[4].Value) ? m.Groups[7].Value : m.Groups[4].Value), @"<[^>]+>", "").Trim(),
                        Rank = int.TryParse(m.Groups[2].Value.TrimEnd('.'), out var rank) ? rank : Regex.Matches(m.Groups[2].Value, @"star\.\w{3,4}").Count(),
                        Timestamp = date,
                        Goals = int.Parse(data[1].Groups[1].Value.Trim()),
                        Assists = int.Parse(data[2].Groups[1].Value.Trim()),
                        PlusMinus = int.Parse(data[3].Groups[1].Value.Trim()),
                    },

                    "GOALIES" => new DailyStarGoalie
                    {
                        LeagueId = league.Id,
                        TeamId = (string.IsNullOrWhiteSpace(m.Groups[3].Value) ? m.Groups[6].Value : m.Groups[3].Value).Trim(),
                        PlayerName = Regex.Replace((string.IsNullOrWhiteSpace(m.Groups[4].Value) ? m.Groups[7].Value : m.Groups[4].Value), @"<[^>]+>", "").Trim(),
                        Rank = int.TryParse(m.Groups[2].Value.TrimEnd('.'), out var rank) ? rank : Regex.Matches(m.Groups[2].Value, @"star\.\w{3,4}").Count(),
                        Timestamp = date,
                        GoalsAgainstAvg = decimal.Parse(data[1].Groups[1].Value.Trim()),
                        Saves = int.Parse(data[2].Groups[1].Value.Trim()),
                        ShotsAgainst = int.Parse(data[3].Groups[1].Value.Trim()),
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
                @"<img[^>]*/team(\d+)\.\w{3,4}[^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .DistinctBy(m => m.Groups[1].Value)
            .Count();

            var picks = Regex.Matches(html,
                @"<td[^>]*>(\d+)</td>\s*<td[^>]*>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>\s*<a[^>]*/member\.(\d+)[^>]*>(.*?)</a>\s*</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>();

            var rounds = (int)Math.Floor((decimal)picks.Count() / teamCount);
            var picksPerRound = (int)Math.Ceiling((decimal)picks.Count() / rounds);

            return picks.Select(m =>
            {
                var overallPick = int.Parse(m.Groups[1].Value);
                var roundPick = overallPick % picksPerRound;

                return new DraftPick
                {
                    LeagueId = league.Id,
                    TeamId = m.Groups[2].Value,
                    PlayerId = m.Groups[3].Value,
                    PlayerName = m.Groups[4].Value.Trim(),
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
                    @"<h4[^>]*sh4[^>]*>(.*?)</h4>",
                    @"<span[^>]*sweekid[^>]*>Week\s*(\d+)</span>\s*(?:<span[^>]*sgamenumber[^>]*>Game\s*#\s*(\d+)</span>)?\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*<a[^>]*&(?:amp;)?gameid=(\d+)[^>]*>\s*<span[^>]*steamname[^>]*>(.*?)</span>\s*<span[^>]*sscore[^>]*>(vs|(\d+)\D+(\d+))</span>\s*<span[^>]*steamname[^>]*>(.*?)</span>\s*</a>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>")})",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                if (!string.IsNullOrWhiteSpace(m.Groups[1].Value))
                {
                    date = ISiteApi.ParseDateWithNoYear(Regex.Replace(m.Groups[1].Value, @"(\d+)[\D\S]{2}", @"$1"), Timezone);
                    return null;
                }

                if (date == null)
                    return null;

                return new Game
                {
                    LeagueId = league.Id,
                    Id = ulong.Parse(m.Groups[5].Value),
                    Timestamp = date.GetValueOrDefault(),
                    VisitorId = m.Groups[4].Value,
                    VisitorScore = int.TryParse(m.Groups[8].Value, out var visitorScore) ? visitorScore : null,
                    HomeId = m.Groups[11].Value,
                    HomeScore = int.TryParse(m.Groups[9].Value, out var homeScore) ? homeScore : null,
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
                @$"<li[^>]*\bcustom-tab-{leagueInfo.LeagueId}\b[^>]*>\s*<a[^>]*forums/[^>]*\.(\d+)[^>]*>.*?<span[^>]*>(.*?)</span>.*?</a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!info.Success)
                return null;

            var season = Regex.Match(html,
                @$"<a[^>]*leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid=(\d+)[^>]*>Roster</a>",
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
                @$"<td[^>]*>\s*<a[^>]*league_draft&(?:amp;)?leagueid={leagueInfo.LeagueId}&(?:amp;)?lgdraftid=(\d+)[^>]*>.*?</a>\s*</td>\s*<td[^>]*>(.*?)</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .LastOrDefault();

            return new Types.Modules.Data.League
            {
                Id = league.Id,
                Name = info.Groups[2].Value.Trim(),
                LogoUrl = $"https://{Domain}/images/league/icon/l{leagueInfo.LeagueId}.png",
                Info = new LeaguegamingLeagueInfo
                {
                    LeagueId = leagueInfo.LeagueId,
                    SeasonId = season.Success ? int.Parse(season.Groups[1].Value) : leagueInfo.SeasonId,
                    ForumId = int.Parse(info.Groups[1].Value),
                    DraftId = draft?.Success == true ? int.Parse(draft.Groups[1].Value) : leagueInfo.DraftId,
                    DraftDate = draft?.Success == true ? ISiteApi.ParseDateTime(draft.Groups[2].Value) : leagueInfo.DraftDate,
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
                @"<li[^>]*\bNewsFeedItem\b[^>]*>(.*?)</li>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var news = Regex.Match(m.Groups[1].Value,
                    @"<a[^>]*\bicon\b[^>]*>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>(?=.*?(?:clinched|eliminated|rights have been acquired))(.*?)</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!news.Success)
                    return null;

                return new News
                {
                    LeagueId = league.Id,
                    TeamId = news.Groups[1].Value,
                    Message = Regex.Replace(Regex.Replace(news.Groups[2].Value, @"<[^>]*>", ""), @" +", " ").Trim(),
                    Timestamp = ISiteApi.ParseDateTime(news.Groups[3].Value, Timezone),
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
                    @"<li[^>]*\bNewsFeedItem\b[^>]*>(.*?)</li>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m =>
                {
                    var ban = Regex.Match(m.Groups[1].Value,
                        @"<a[^>]*>\s*<img[^>]*/(?:team(\d+)|l\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*<span[^>]*>(.*?)</span>\s*has been issued a\s*<span[^>]*>.*? Ban</span>\s*in Season \d+ of the\s*<span[^>]*>.*?</span>\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (ban.Success == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { ban.Groups[1].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { ban.Groups[2].Value.Trim() },
                            Type = RosterTransactionType.Banned,
                            Timestamp = ISiteApi.ParseDateTime(ban.Groups[3].Value, Timezone),
                        };
                    }

                    var callUp = Regex.Match(m.Groups[1].Value,
                        @"<a[^>]*>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>.*?<a[^>]*>\s*<img[^>]*/arrow1\.\w{3,4}[^>]*>.*?<a[^>]*>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*The\s*<img[^>]*/team\2\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*have sent\s*<span[^>]*>(.*?)</span>\s*to the\s*<img[^>]*/team\1\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (callUp.Success == true && teamIds.Contains(callUp.Groups[1].Value) == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { callUp.Groups[1].Value, callUp.Groups[2].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { callUp.Groups[3].Value.Trim() },
                            Type = RosterTransactionType.CalledUp,
                            Timestamp = ISiteApi.ParseDateTime(callUp.Groups[4].Value, Timezone),
                        };
                    }

                    var placedOnIr = Regex.Match(m.Groups[1].Value,
                        @"<a[^>]*>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*<span[^>]*>(.*?)</span>\s*has been moved to the\s*<span[^>]*>Injured Reserve</span>\s*list\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (placedOnIr.Success == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { placedOnIr.Groups[1].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { placedOnIr.Groups[2].Value.Trim() },
                            Type = RosterTransactionType.PlacedOnIr,
                            Timestamp = ISiteApi.ParseDateTime(placedOnIr.Groups[3].Value, Timezone),
                        };
                    }

                    var removedFromIr = Regex.Match(m.Groups[1].Value,
                        @"<a[^>]*>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*<span[^>]*>(.*?)</span>\s*has been taken off the\s*<span[^>]*>Injured Reserve</span>\s*list\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (removedFromIr.Success == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { removedFromIr.Groups[1].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { removedFromIr.Groups[2].Value.Trim() },
                            Type = RosterTransactionType.RemovedFromIr,
                            Timestamp = ISiteApi.ParseDateTime(removedFromIr.Groups[3].Value, Timezone),
                        };
                    }

                    var sendDown = Regex.Match(m.Groups[1].Value,
                        @"<a[^>]*>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>.*?<a[^>]*>\s*<img[^>]*/arrow2\.\w{3,4}[^>]*>.*?<a[^>]*>\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*The\s*<img[^>]*/team\1\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*have sent\s*<span[^>]*>(.*?)</span>\s*to the\s*<img[^>]*/team\2\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (sendDown.Success == true && teamIds?.Contains(sendDown.Groups[1].Value) == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { sendDown.Groups[1].Value, sendDown.Groups[2].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { sendDown.Groups[3].Value.Trim() },
                            Type = RosterTransactionType.SentDown,
                            Timestamp = ISiteApi.ParseDateTime(sendDown.Groups[4].Value, Timezone),
                        };
                    }

                    var suspension = Regex.Match(m.Groups[1].Value,
                        @"<a[^>]*>\s*<img[^>]*/(?:team(\d+)|l\d+)\.\w{3,4}[^>]*>\s*</a>\s*<div[^>]*>\s*<h3[^>]*>\s*<span[^>]*>(.*?)</span>\s*has been issued a\s*<span[^>]*>.*? Suspension</span>\s*in Season \d+ of the\s*<span[^>]*>.*?</span>\s*</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>\s*</div>",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (suspension.Success == true)
                    {
                        return new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { suspension.Groups[1].Value }.Where(t => !string.IsNullOrWhiteSpace(t) && t != "0").ToArray(),
                            PlayerNames = new string[] { suspension.Groups[2].Value.Trim() },
                            Type = RosterTransactionType.Suspended,
                            Timestamp = ISiteApi.ParseDateTime(suspension.Groups[3].Value, Timezone),
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
            @$"<div[^>]*\bteam_box_icon\b[^>]*>.*?<a[^>]*page=team_page&(?:amp;)?teamid=(\d+)&(?:amp;)?leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid={leagueInfo.SeasonId}[^>]*>(.*?)</a>\s*</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .Cast<Match>()
        .Select(m => m.Groups[1].Value);
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
                @$"<div[^>]*\bteam_box_icon\b[^>]*>.*?<a[^>]*page=team_page&(?:amp;)?teamid=(\d+)&(?:amp;)?leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid={leagueInfo.SeasonId}[^>]*>(.*?)</a>\s*</div>",
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
                @$"<td[^>]*>\s*<img[^>]*/team\d+\.\w{{3,4}}[^>]*>\s*\d+\)\s*.*?\*?<a[^>]*page=team_page&(?:amp;)?teamid=(\d+)&(?:amp;)?leagueid=(?:{leagueInfo.LeagueId})?&(?:amp;)?seasonid=(?:{leagueInfo.SeasonId})?[^>]*>(.*?)</a>\s*</td>",
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

                return new Trade
                {
                    LeagueId = league.Id,
                    FromId = trade.Groups[1].Value,
                    ToId = trade.Groups[3].Value,
                    FromAssets = Regex.Split(Regex.Replace(trade.Groups[2].Value, @"<[^>]*>", ""), @"\s*&\s*").Select(a => a.Trim()).Where(a => a.ToLower() != "nothing").ToArray(),
                    ToAssets = Regex.Split(Regex.Replace(trade.Groups[4].Value, @"<[^>]*>", ""), @"\s*&\s*").Select(a => a.Trim()).Where(a => a.ToLower() != "nothing").ToArray(),
                    Timestamp = ISiteApi.ParseDateTime(trade.Groups[5].Value, Timezone),
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
                @"<li[^>]*\bNewsFeedItem\b[^>]*>(.*?)</li>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var waiver = Regex.Match(m.Groups[1].Value,
                    @$"<h3[^>]*>.*?(?:{string.Join("|",
                        @"<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>\s*have (placed|removed|claimed)\s*<span[^>]*>(.*?)</span>",
                        @"<span[^>]*>(.*?)</span>\s*has cleared waivers and is reporting to.*?\s*<img[^>]*/team(\d+)\.\w{3,4}[^>]*>\s*<span[^>]*>.*?</span>"
                    )}).*?</h3>\s*<abbr[^>]*\bDateTime\b[^>]*>(.*?)</abbr>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!waiver.Success)
                    return null;

                return new Waiver
                {
                    LeagueId = league.Id,
                    TeamId = string.Join("", waiver.Groups[1].Value, waiver.Groups[5].Value).Trim(),
                    PlayerName = string.Join("", waiver.Groups[3].Value, waiver.Groups[4].Value).Trim(),
                    Type = Enum.TryParse<WaiverActionType>(waiver.Groups[2].Value, true, out var action) ? action : WaiverActionType.Cleared,
                    Timestamp = ISiteApi.ParseDateTime(waiver.Groups[6].Value, Timezone),
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