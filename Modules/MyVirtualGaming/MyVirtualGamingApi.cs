using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Duthie.Types.Modules.Api;
using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Modules.MyVirtualGaming;

public class MyVirtualGamingApi
    : IBidApi, IContractApi, IDraftApi, IGameApi, ILeagueApi, IRosterApi, ITeamApi, ITradeApi
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
                var player = Regex.Match(m.Groups[2].Value, @"<a[^>]*player&id=(\d+)[^>]*>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                return new Bid
                {
                    LeagueId = league.Id,
                    TeamId = m.Groups[1].Value,
                    PlayerId = player.Groups[1].Value,
                    PlayerName = player.Groups[2].Value.Trim(),
                    Amount = ISiteApi.ParseDollars(Regex.Match(m.Groups[2].Value, @"\$[\d\.]+( \w)?", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[0].Value),
                    State = BidState.Won,
                    Timestamp = ISiteApi.ParseDateTime(m.Groups[3].Value, Timezone),
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
                        TeamId = m.Groups[1].Value,
                        PlayerName = contract.Groups[1].Value.Trim(),
                        Amount = ISiteApi.ParseDollars(contract.Groups[2].Value),
                        Timestamp = ISiteApi.ParseDateTime(m.Groups[3].Value, Timezone),
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
                    PlayerId = m.Groups[3].Value,
                    PlayerName = m.Groups[4].Value.Trim(),
                    RoundNumber = roundNumber,
                    RoundPick = roundPicks++,
                    OverallPick = int.Parse(m.Groups[1].Value),
                });
            })
            .SelectMany(d => d);
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching draft picks for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    private async Task<IDictionary<string, string>> GetTeamLookupAsync(League league, bool includeAffiliates = false)
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

            if (includeAffiliates && leagueInfo.AffiliatedLeagueIds.Count() > 0)
                html += string.Join("", await Task.WhenAll(
                    leagueInfo.AffiliatedLeagueIds.Select(leagueId => _httpClient.GetStringAsync($"https://{Domain}/vghlleagues/{leagueId}/standings"))));

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
                        date = ISiteApi.ParseDateTime(Regex.Replace(m.Groups[1].Value, @"^(\d+).{2} (\S+) (\d+) @ (.*?)", @"$2 $1, $3 $4"), Timezone);
                        return null;
                    }

                    if (date == null)
                        return null;

                    return new Game
                    {
                        LeagueId = league.Id,
                        Id = ulong.Parse(m.Groups[2].Value),
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
                LogoUrl = league.LogoUrl,
                Info = new MyVirtualGamingLeagueInfo
                {
                    Features = features,
                    LeagueId = leagueId,
                    SeasonId = seasonId ?? leagueInfo.SeasonId,
                    ScheduleId = scheduleId ?? leagueInfo.ScheduleId,
                    AffiliatedLeagueIds = leagueInfo.AffiliatedLeagueIds,
                },
            };
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching info for league \"{league.Name}\" [{league.Id}]", e);
        }
    }

    public async Task<IEnumerable<RosterTransaction>?> GetRosterTransactionsAsync(League league)
    {
        try
        {
            if (!IsSupported(league))
                return null;

            var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

            if (!leagueInfo.Features.HasFlag(MyVirtualGamingFeatures.RecentTransactions))
                return new List<RosterTransaction>();

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "recent-transactions"));

            var lookup = await GetTeamLookupAsync(league, includeAffiliates: true);

            return Regex.Matches(html,
                @"<div[^>]*\b(irs|inactives|callup_senddown|drops)\b[^>]*>.*?<tbody[^>]*>(.*?)</tbody>\s*</table>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var type = m.Groups[1].Value.Trim();

                return type.ToUpper() switch
                {
                    "IRS" => Regex.Matches(m.Groups[2].Value,
                            @"<td[^>]*>\s*<a[^>]*rosters\?id=(\d+)[^>]*>.*?</a>\s*</td>\s*<td[^>]*>\s*(Placed|Removed)\s*<a[^>]*player&(?:amp;)?id=(\d+)[^>]*>(.*?)</a>\s*.*? from injured reserved\s*</td>\s*<td[^>]*>(.*?)</td>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                        .Cast<Match>()
                        .Select(m => new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { m.Groups[1].Value },
                            PlayerIds = new string[] { m.Groups[3].Value },
                            PlayerNames = new string[] { m.Groups[4].Value.Trim() },
                            Type = m.Groups[1].Value.ToLower().Contains("placed")
                                ? RosterTransactionType.PlacedOnIr
                                : RosterTransactionType.RemovedFromIr,
                            Timestamp = ISiteApi.ParseDateTime(m.Groups[5].Value, Timezone),
                        }
                        )
                        .Cast<RosterTransaction>(),

                    "INACTIVES" => Regex.Matches(m.Groups[2].Value,
                            @"<td[^>]*>\s*<a[^>]*player&(?:amp;)?id=(\d+)[^>]*>(.*?)</a>\s*</td>\s*<td[^>]*>\s*Has been reported inactive.*?<a[^>]*rosters\?id=(\d+)[^>]*>.*?</a>.*?</td>\s*<td[^>]*>(.*?)</td>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                        .Cast<Match>()
                        .Select(m => new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { m.Groups[3].Value },
                            PlayerIds = new string[] { m.Groups[1].Value },
                            PlayerNames = new string[] { m.Groups[2].Value.Trim() },
                            Type = RosterTransactionType.ReportedInactive,
                            Timestamp = ISiteApi.ParseDateTime(m.Groups[4].Value, Timezone),
                        })
                        .Cast<RosterTransaction>(),

                    "CALLUP_SENDDOWN" => Regex.Matches(m.Groups[2].Value,
                            @"<td[^>]*>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*<i[^>]*>\s*</i>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>.*?have (called up|sent down) (.*?) \S+/\S+ .*? (?:from|to) .*?</td>\s*<td[^>]*>(.*?)</td>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                        .Cast<Match>()
                        .Select(m => new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { lookup[m.Groups[1].Value.Trim()], lookup[m.Groups[2].Value.Trim()] },
                            PlayerNames = new string[] { m.Groups[4].Value.Trim() },
                            Type = m.Groups[3].Value.ToLower().Contains("called up")
                                ? RosterTransactionType.CalledUp
                                : RosterTransactionType.SentDown,
                            Timestamp = ISiteApi.ParseDateTime(m.Groups[5].Value, Timezone),
                        })
                        .Cast<RosterTransaction>(),

                    "DROPS" => Regex.Matches(m.Groups[2].Value,
                            @"<td[^>]*>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>.*?dropped (.*?) \S+/\S+ .*?</td>\s*<td[^>]*>(.*?)</td>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                        .Cast<Match>()
                        .Select(m => new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { lookup[m.Groups[1].Value.Trim()] },
                            PlayerNames = new string[] { m.Groups[2].Value.Trim() },
                            Type = RosterTransactionType.Banned,
                            Timestamp = ISiteApi.ParseDateTime(m.Groups[3].Value, Timezone),
                        })
                        .Cast<RosterTransaction>(),

                    _ => new List<RosterTransaction>()
                };
            })
            .SelectMany(r => r)
            .Where(r => r != null);
        }
        catch (Exception e)
        {
            throw new ApiException($"An unexpected error occurred while fetching roster transactions for league \"{league.Name}\" [{league.Id}]", e);
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
                var trade = Regex.Match(m.Groups[3].Value, @"The .*? have traded (.*?)\s*(\w+/\w+ .*?\$[\d,.]+)?\s*to the .*?.", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!trade.Success || !lookup.ContainsKey(m.Groups[1].Value.Trim()) || !lookup.ContainsKey(m.Groups[2].Value.Trim()))
                    return null;

                return new Trade
                {
                    LeagueId = league.Id,
                    FromId = lookup[m.Groups[1].Value.Trim()],
                    ToId = lookup[m.Groups[2].Value.Trim()],
                    FromAssets = new string[] { Regex.Replace(trade.Groups[1].Value.Trim(), @"the (.*? \d+\S+) round draft pick", @"$1 Round Pick") },
                    Timestamp = ISiteApi.ParseDateTime(m.Groups[4].Value, Timezone),
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

    public string? GetRosterTransactionUrl(League league, RosterTransaction rosterTransaction)
    {
        if (!IsSupported(league))
            return null;

        var slug = rosterTransaction.Type switch
        {
            RosterTransactionType.PlacedOnIr or RosterTransactionType.RemovedFromIr => "irs",
            RosterTransactionType.ReportedInactive => "inactives",
            RosterTransactionType.CalledUp or RosterTransactionType.SentDown => "callup_senddown",
            RosterTransactionType.Banned => "drops",
            _ => "",
        };

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;
        return $"https://{Domain}/vghlleagues/{leagueInfo.LeagueId}/recent-transactions#{slug}";
    }

    public string? GetTradeUrl(League league, Trade trade)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;
        return $"https://{Domain}/vghlleagues/{leagueInfo.LeagueId}/recent-transactions#Trades";
    }
}