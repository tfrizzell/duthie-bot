using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Duthie.Types.Modules.Api;
using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Modules.MyVirtualGaming;

public class MyVirtualGamingApi
    : IBidApi, IContractApi, IDraftApi, IGameApi, ILeagueApi, ITeamApi, ITradeApi
{
    private const string Domain = "vghl.myvirtualgaming.com";
    private static readonly TimeZoneInfo Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private readonly HttpClient _httpClient = new HttpClient();

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { MyVirtualGamingSiteProvider.VGHL.Id };
    }

    private string GetUrl(League league, string path, IDictionary<string, object?>? parameters = null)
    {
        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;
        var queryString = parameters == null ? string.Empty
            : string.Join("&", parameters.Where(p => p.Value != null).Select(p => string.Join("=", HttpUtility.UrlEncode(p.Key), HttpUtility.UrlEncode(p.Value!.ToString()))));
        return Regex.Replace($"https://{Domain}/vghlleagues/{leagueInfo.LeagueId}/{path}?{queryString}".Replace("?&", "?"), @"[?&]+$", "");
    }

    private bool IsSupported(League league) =>
        Supports.Contains(league.SiteId) || league.Info is MyVirtualGamingLeagueInfo;

    public async Task<IEnumerable<Bid>?> GetBidsAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

            if (!leagueInfo.Features.HasFlag(MyVirtualGamingFeatures.RecentTransactions))
                return new List<Bid>();

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "recent-transactions"));

            var closedBids = Regex.Match(html,
                @"<div[^>]*\bclosed-bids\b[^>]*>.*?<tbody[^>]*>(.*?)</tbody>\s*</table>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!closedBids.Success)
                return new List<Bid>();

            return Regex.Matches(closedBids.Groups[1].Value,
                @"<td[^>]*>\s*<a[^>]*id=(\d+)[^>]*>\s*<img[^>]*>.*?</a>\s*</td>\s*<td[^>]*>(.*?)</td>\s*<td[^>]*>.*?</td>\s*<td[^>]*>.*?</td>\s*<td[^>]*>(.*?)</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var dateTime = DateTime.Parse(m.Groups[3].Value.Trim());
                var player = Regex.Match(m.Groups[2].Value, @"<a[^>]*player&id=(\d+)[^>]*>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                return new Bid
                {
                    LeagueId = league.Id,
                    TeamId = m.Groups[1].Value.Trim(),
                    PlayerId = player.Groups[1].Value.Trim(),
                    PlayerName = player.Groups[2].Value.Trim(),
                    Amount = ISiteApi.ParseDollars(Regex.Match(m.Groups[2].Value, @"\$[\d\.]+( \w)?", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[0].Value),
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

    public async Task<IEnumerable<Contract>?> GetContractsAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

            if (!leagueInfo.Features.HasFlag(MyVirtualGamingFeatures.RecentTransactions))
                return new List<Contract>();

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "recent-transactions"));

            return Regex.Matches(html,
                @"<div[^>]*\b(contracts|signings)\b[^>]*>.*?<tbody[^>]*>(.*?)</tbody>\s*</table>\s*</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(c =>
                Regex.Matches(c.Groups[2].Value,
                    @"<td[^>]*>\s*<a[^>]*id=(\d+)[^>]*>\s*<img[^>]*>.*?</a>\s*</td>\s*<td[^>]*>(.*?)</td>\s*<td[^>]*>(.*?)</td>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m =>
                {
                    var dateTime = DateTime.Parse(m.Groups[3].Value.Trim());

                    var contract = Regex.Match(m.Groups[2].Value.Trim(),
                        m.Groups[1].Value.ToLower() == "signing"
                            ? @"(.*?)\s+has\s+been\s+signed\s+to\s+a\s+(\$[\d,.])\s+.*?\s+with\s+the\s+.*?\s+during\s+season\s+\d+"
                            : @"The\s+.*?\s+have\s+promoted\s+(.*?)\s+\w+/\w+\s+.*?\s+with\s+a\s+contract\s+amount\s+of\s+(\$[\d,.]+)",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (!contract.Success)
                        return null;

                    return new Contract
                    {
                        LeagueId = league.Id,
                        TeamId = m.Groups[1].Value.Trim(),
                        PlayerName = contract.Groups[1].Value.Trim(),
                        Amount = ISiteApi.ParseDollars(contract.Groups[2].Value),
                        Timestamp = new DateTimeOffset(dateTime, Timezone.GetUtcOffset(dateTime)),
                    };
                }))
            .SelectMany(c => c)
            .Where(c => c != null)
            .Cast<Contract>();
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching contracts for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<DraftPick>?> GetDraftPicksAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

            if (!leagueInfo.Features.HasFlag(MyVirtualGamingFeatures.DraftCentre))
                return new List<DraftPick>();

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "draft-centre"));

            var teams = await GetTeamLookupAsync(league);

            return Regex.Matches(html,
                @"<div[^>]*\bround(\d+)\b[^>]*>.*?<tbody[^>]*>(.*?)</tbody>\s*</table>\s*</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(d =>
            {
                var roundNumber = int.Parse(d.Groups[1].Value.Trim());
                var roundPicks = 1;

                return Regex.Matches(d.Groups[2].Value,
                    @"<td[^>]*>(\d+)</td>\s*<td[^>]*>.*?</td>\s*<td[^>]*>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>\s*<a[^>]*player&id=(\d+)[^>]*>(.*?)</a>\s*</td>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m => new DraftPick
                {
                    LeagueId = league.Id,
                    TeamId = teams[m.Groups[2].Value.Trim()],
                    PlayerId = m.Groups[3].Value.Trim(),
                    PlayerName = m.Groups[4].Value.Trim(),
                    RoundNumber = roundNumber,
                    RoundPick = roundPicks++,
                    OverallPick = int.Parse(m.Groups[1].Value.Trim()),
                });
            })
            .SelectMany(c => c);
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

            var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
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
                return new List<Game>();

            var teams = await GetTeamLookupAsync(league);

            return (await Task.WhenAll(weeks.Select(async week =>
            {
                var _html = await _httpClient.GetStringAsync(GetUrl(league,
                    path: "schedule",
                    parameters: new Dictionary<string, object?>
                    {
                        ["single_seasons"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
                        ["filter_scheduled_week"] = week,
                    }));

                DateTimeOffset? date = null;

                return Regex.Matches(_html,
                    @$"(?:{string.Join("|",
                        @"(\d+.{2} \S+ \d{4} @ \d+:\d+[ap]m)",
                        @"<div[^>]*\bgame_div_(\d+)\b[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*</div>\s*<div[^>]*>.*?</div>\s*<div[^>]*\bschedule-team-score\b[^>]*>\s*(\d+|-)\s*</div>\s*</div>\s*<div[^>]*>\s*<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*</div>\s*<div[^>]*>.*?</div>\s*<div[^>]*\bschedule-team-score\b[^>]*>\s*(\d+|-)\s*</div>\s*</div>\s*<div[^>]*>.*?</div>\s*</div>\s*<div[^>]*\bschedule-summary-link\b[^>]*>\s*<a[^>]*>(Final|Stats)(?:/(OT|SO))?</a>\s*</div>")})",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m =>
                {
                    if (!string.IsNullOrWhiteSpace(m.Groups[1].Value))
                    {
                        var dateTime = DateTime.Parse(Regex.Replace(m.Groups[1].Value, @"^(\d+).{2} (\S+) (\d+) @ (.*?)", @"$2 $1, $3 $4"));
                        date = new DateTimeOffset(dateTime, Timezone.GetUtcOffset(dateTime));
                        return null;
                    }

                    if (date == null)
                        return null;

                    return new Game
                    {
                        LeagueId = league.Id,
                        Id = ulong.Parse(m.Groups[2].Value.Trim()),
                        Timestamp = date.GetValueOrDefault(),
                        VisitorId = teams[m.Groups[3].Value.Trim()],
                        VisitorScore = int.TryParse(m.Groups[4].Value, out var visitorScore) ? visitorScore : null,
                        HomeId = teams[m.Groups[5].Value.Trim()],
                        HomeScore = int.TryParse(m.Groups[6].Value, out var homeScore) ? homeScore : null,
                        Overtime = m.Groups[8].Value.ToUpper().Contains("OT"),
                        Shootout = m.Groups[8].Value.ToUpper().Contains("SO"),
                    };
                })
                .Where(g => g != null)
                .Cast<Game>();
            }))).SelectMany(g => g);
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

            var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

            var xml = await _httpClient.GetStringAsync(GetUrl(league,
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

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "schedule"));

            var logo = Regex.Match(html,
                @$"<div[^>]*\bbarlogo\b[^>]*>\s*<a[^>]*vghlleagues/{leagueInfo.LeagueId}/{leagueInfo.LeagueId}[^>]*>\s*<img[^>]*src=[""'](.*?)[""'][^>]*>\s*</a>\s*</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            var seasonId = Regex.Matches(
                Regex.Match(html,
                    @"<select[^>]*\bsingle_seasons\b[^>]*>(.*?)</select>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value,
                @"<option[^>]*value=[""']?(\d+)[""']?[^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .OrderBy(m => Regex.Match(m.Groups[0].Value, @"<option[^>]*\bselected\b[^>]>").Success)
                .ThenBy(m => int.Parse(m.Groups[1].Value))
            .TakeLast(1)
            .Select(m => int.Parse(m.Groups[1].Value))
            .Cast<int?>()
            .FirstOrDefault();

            html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "standings"));

            var scheduleId = Regex.Matches(
                Regex.Match(html,
                    @"<select[^>]*\bfilter_schedule\b[^>]*>(.*?)</select>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value,
                @"<option[^>]*value=[""']?(\d+)[""']?[^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .OrderBy(m => Regex.Match(m.Groups[0].Value, @"<option[^>]*\bselected\b[^>]>").Success)
                .ThenBy(m => int.Parse(m.Groups[1].Value))
            .TakeLast(1)
            .Select(m => int.Parse(m.Groups[1].Value))
            .Cast<int?>()
            .FirstOrDefault();

            var leagueId = Regex.Split(id.InnerText.Trim(), @"/+")[3] ?? leagueInfo.LeagueId;
            var features = MyVirtualGamingFeatures.None;

            if (Regex.Match(html, @$"/vghlleagues/{leagueId}/recent-transactions", RegexOptions.IgnoreCase | RegexOptions.Singleline).Success)
                features |= MyVirtualGamingFeatures.RecentTransactions;

            if (Regex.Match(html, @$"/vghlleagues/{leagueId}/draft-centre", RegexOptions.IgnoreCase | RegexOptions.Singleline).Success)
                features |= MyVirtualGamingFeatures.DraftCentre;

            return new Types.Modules.Data.League
            {
                Id = league.Id,
                Name = Regex.Replace(title.InnerText.Trim(), @"\s+Home$", ""),
                LogoUrl = logo.Success ? $"https://{Domain}/{Regex.Replace(logo.Groups[1].Value.Trim(), @$"^(https://{Domain})?/?", "")}" : league.LogoUrl,
                Info = new MyVirtualGamingLeagueInfo
                {
                    Features = features,
                    LeagueId = leagueId,
                    SeasonId = seasonId ?? leagueInfo.SeasonId,
                    ScheduleId = scheduleId ?? leagueInfo.ScheduleId,
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

            var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "player-statistics",
                parameters: new Dictionary<string, object?>
                {
                    ["filter_schedule"] = leagueInfo.ScheduleId > 0 ? leagueInfo.ScheduleId : null,
                }));

            var nameMatches = Regex.Matches(
                Regex.Match(html,
                    @"<select[^>]*\bfilter_stat_team\b[^>]*>(.*?)</select>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value,
                @"<option[^>]*value=[""']?(\d+)[""']?[^>]*>(.*?)</option>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (nameMatches.Count() == 0)
                return null;

            var lookup = await GetTeamLookupAsync(league);

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

            html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "schedule",
                parameters: new Dictionary<string, object?>
                {
                    ["single_seasons"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
                }));

            var shortNameMatches = Regex.Matches(html,
                @"<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*</div>\s*<div[^>]*\bschedule-team\b[^>]*>\s*<div[^>]*\bschedule-team-name\b[^>]*>(.*?)</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .DistinctBy(m => m.Groups[1].Value);

            foreach (var match in shortNameMatches)
            {
                if (!lookup.ContainsKey(match.Groups[1].Value))
                    continue;

                var id = lookup[match.Groups[1].Value];

                if (!teams.ContainsKey(id))
                    teams.Add(id, new Team { LeagueId = league.Id, Id = id });

                teams[id].ShortName = match.Groups[2].Value.Trim();

                if (string.IsNullOrWhiteSpace(teams[id].Name))
                    teams[id].Name = teams[id].ShortName;
            }

            return FixTeams(teams.Values
                .Where(t => lookup.Values.Contains(t.Id))
                .ToList());
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching teams for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    private async Task<IDictionary<string, string>> GetTeamLookupAsync(League league)
    {
        try
        {
            var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "standings",
                parameters: new Dictionary<string, object?>
                {
                    ["filter_schedule"] = leagueInfo.ScheduleId > 0 ? leagueInfo.ScheduleId : null,
                }));

            var lookup = Regex.Matches(html,
                @"<a[^>]*/rosters\?id=(\d+)[^>]*>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*<\/a>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .DistinctBy(m => m.Groups[2].Value.ToUpper())
            .ToDictionary(
                m => m.Groups[2].Value.ToUpper(),
                m => m.Groups[1].Value,
                StringComparer.OrdinalIgnoreCase);

            if (lookup.ContainsKey("TAP") && !lookup.ContainsKey("TAPP"))
                lookup.Add("TAPP", lookup["TAP"]);

            if (!lookup.ContainsKey("TAP") && lookup.ContainsKey("TAPP"))
                lookup.Add("TAP", lookup["TAPP"]);

            return lookup;
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching team lookup for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    private IEnumerable<Team> FixTeams(List<Team> teams)
    {
        var leagueId = teams.FirstOrDefault()?.LeagueId;

        if (leagueId == MyVirtualGamingLeagueProvider.VGNHL.Id)
        {
            teams.AddRange(
                teams.Where(t => t.Name == "Nashville Nashville")
                    .ToList()
                    .Select(t => new Team
                    {
                        LeagueId = t.LeagueId,
                        Id = t.Id,
                        Name = "Nashville Predators",
                        ShortName = "Predators",
                    }));
        }
        else if (leagueId == MyVirtualGamingLeagueProvider.VGAHL.Id)
        {
            teams.AddRange(
                teams.Where(t => t.Name == "Bellevile Senators")
                    .ToList()
                    .Select(t => new Team
                    {
                        LeagueId = t.LeagueId,
                        Id = t.Id,
                        Name = "Belleville Senators",
                        ShortName = "Senators",
                    }));
        }

        return teams;
    }

    public async Task<IEnumerable<Trade>?> GetTradesAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

            if (!leagueInfo.Features.HasFlag(MyVirtualGamingFeatures.RecentTransactions))
                return new List<Trade>();

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "recent-transactions"));

            var trades = Regex.Match(html,
                @"<div[^>]*\bTrades\b[^>]*>.*?<tbody[^>]*>(.*?)</tbody>\s*</table>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!trades.Success)
                return new List<Trade>();

            var lookup = await GetTeamLookupAsync(league);

            return Regex.Matches(trades.Groups[1].Value,
                @"<td[^>]*>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*<i[^>]*>\s*</i>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>(.*?)</td>\s*<td[^>]*>(.*?)</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var dateTime = DateTime.Parse(m.Groups[4].Value.Trim());
                var trade = Regex.Match(m.Groups[3].Value, @"The .*? have traded (.*?)\s*(\w+/\w+ .*?\$[\d,.]+)?\s*to the .*?.", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!trade.Success || !lookup.ContainsKey(m.Groups[1].Value.Trim()) || !lookup.ContainsKey(m.Groups[2].Value.Trim()))
                    return null;

                return new Trade
                {
                    LeagueId = league.Id,
                    FromId = lookup[m.Groups[1].Value.Trim()],
                    ToId = lookup[m.Groups[2].Value.Trim()],
                    FromAssets = new string[] { Regex.Replace(trade.Groups[1].Value.Trim(), @"the (.*? \d+\S+) round draft pick", @"$1 Round Pick") },
                    Timestamp = new DateTimeOffset(dateTime, Timezone.GetUtcOffset(dateTime)),
                };
            })
            .Where(t => t != null)
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

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;
        return $"https://{Domain}/vghlleagues/{leagueInfo.LeagueId}/recent-transactions#closed-bids";
    }

    public string? GetContractUrl(League league, Contract contract)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;
        return $"https://{Domain}/vghlleagues/{leagueInfo.LeagueId}/recent-transactions#Signings";
    }

    public string? GetDraftPickUrl(League league, DraftPick draftPick)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;
        return $"https://{Domain}/vghlleagues/{leagueInfo.LeagueId}/draft-centre";
    }

    public string? GetGameUrl(League league, Game game)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;
        return $"https://{Domain}/vghlleagues/{leagueInfo.LeagueId}/schedule?view=game&layout=game&id={game.Id}";
    }

    public string? GetTradeUrl(League league, Trade trade)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;
        return $"https://{Domain}/vghlleagues/{leagueInfo.LeagueId}/recent-transactions#Trades";
    }
}