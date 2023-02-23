using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Duthie.Extensions;
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
        return Regex.Replace($"https://{Domain}/vghlleagues/{leagueInfo.LeagueId}/{Regex.Replace(path, @"^/+", "")}?{queryString}".Replace("?&", "?"), @"[?&]+$", "");
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
                @"<div[^>]*\bclosed-bids\b[^>]*>.*?<tbody[^>]*>(?<records>.*?)</tbody>\s*</table>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!closedBids.Success)
                return new List<Bid>();

            return Regex.Matches(closedBids.Groups["records"].Value,
                @"<td[^>]*>\s*<a[^>]*id=(?<teamId>\d+)[^>]*>\s*<img[^>]*>.*?</a>\s*</td>\s*<td[^>]*>(?<info>.*?)</td>\s*<td[^>]*>.*?</td>\s*<td[^>]*>.*?</td>\s*<td[^>]*>(?<timestamp>.*?)</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var player = Regex.Match(m.Groups["info"].Value, @"<a[^>]*player&(?:amp;)?id=(?<playerId>\d+)[^>]*>(?<playerName>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                return new Bid
                {
                    LeagueId = league.Id,
                    TeamId = m.Groups["teamId"].Value,
                    PlayerId = player.Groups["playerId"].Value,
                    PlayerName = player.Groups["playerName"].Value.Trim(),
                    Amount = ISiteApi.ParseDollars(Regex.Match(m.Groups["info"].Value, @"\$[\d\.]+( \w)?", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[0].Value),
                    State = BidState.Won,
                    Timestamp = ISiteApi.ParseDateTime(m.Groups["timestamp"].Value, TimeZoneInfo.Utc),
                };
            })
            .Reverse()
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

            var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

            if (!leagueInfo.Features.HasFlag(MyVirtualGamingFeatures.RecentTransactions))
                return new List<Contract>();

            var html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "recent-transactions"));

            return Regex.Matches(html,
                @"<div[^>]*\b(?<type>contracts|signings)\b[^>]*>.*?<tbody[^>]*>(?<records>.*?)</tbody>\s*</table>\s*</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(c =>
                Regex.Matches(c.Groups["records"].Value,
                    @"<td[^>]*>\s*<a[^>]*id=(?<teamId>\d+)[^>]*>\s*<img[^>]*>.*?</a>\s*</td>\s*<td[^>]*>(?<contract>.*?)</td>\s*<td[^>]*>(?<timestamp>.*?)</td>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m =>
                {
                    var contract = Regex.Match(m.Groups["contract"].Value.Trim(),
                        m.Groups["type"].Value.ToLower() == "signing"
                            ? @"(?<playerName>.*?)\s+has\s+been\s+signed\s+to\s+a\s+(?<playerContract>\$[\d,.])\s+.*?\s+with\s+the\s+.*?\s+during\s+season\s+\d+"
                            : @"The\s+.*?\s+have\s+promoted\s+(?<playerName>.*?)\s+\w+/\w+\s+.*?\s+with\s+a\s+contract\s+amount\s+of\s+(?<playerContract>\$[\d,.]+)",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);

                    if (!contract.Success)
                        return null;

                    return new Contract
                    {
                        LeagueId = league.Id,
                        TeamId = m.Groups["teamId"].Value,
                        PlayerName = contract.Groups["playerName"].Value.Trim(),
                        Amount = ISiteApi.ParseDollars(contract.Groups["playerContract"].Value),
                        Timestamp = ISiteApi.ParseDateTime(m.Groups["timestamp"].Value, TimeZoneInfo.Utc),
                    };
                }))
            .SelectMany(c => c)
            .Where(c => c != null)
            .Cast<Contract>()
            .Reverse()
            .ToList();
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
                @"<div[^>]*\bround(?<roundNumber>\d+)\b[^>]*>.*?<tbody[^>]*>(?<records>.*?)</tbody>\s*</table>\s*</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(d =>
            {
                var roundNumber = int.Parse(d.Groups["roundNumber"].Value.Trim());
                var roundPicks = 1;

                return Regex.Matches(d.Groups["records"].Value,
                    @"<td[^>]*>(?<pickNumber>\d+)</td>\s*<td[^>]*>.*?</td>\s*<td[^>]*>\s*<img[^>]*/(?<teamAbbrev>\w+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>\s*<a[^>]*player&(?:amp;)?id=(?<playerId>\d+)[^>]*>(?<playerName>.*?)</a>\s*</td>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Cast<Match>()
                .Select(m => new DraftPick
                {
                    LeagueId = league.Id,
                    TeamId = teams[m.Groups["teamAbbrev"].Value.Trim()],
                    PlayerId = m.Groups["playerId"].Value,
                    PlayerName = m.Groups["playerName"].Value.Trim(),
                    RoundNumber = roundNumber,
                    RoundPick = roundPicks++,
                    OverallPick = int.Parse(m.Groups["pickNumber"].Value),
                });
            })
            .SelectMany(d => d)
            .Reverse()
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

            var teams = await GetTeamLookupAsync(league);
            return (await GetRegularSeasonGamesAsync(league, teams))
                .Concat(await GetPlayoffGamesAsync(league, teams));
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
                    @"<select[^>]*\bsingle_seasons\b[^>]*>(?<seasons>.*?)</select>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["seasons"].Value,
                @"<option(?=[^>]*>\sselected)[^>]*value=[""']?(?<seasonId>\d+)[""']?[^>]*>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .OrderBy(m => Regex.Match(m.Groups[0].Value, @"<option[^>]*\bselected\b[^>]>").Success)
                .ThenBy(m => int.Parse(m.Groups["seasonId"].Value))
            .TakeLast(1)
            .Select(m => int.Parse(m.Groups["seasonId"].Value))
            .Cast<int?>()
            .FirstOrDefault();

            html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "player-statistics"));

            var scheduleId = Regex.Matches(
                Regex.Match(html,
                    @"<select[^>]*\bfilter_schedule\b[^>]*>(?<schedules>.*?)</select>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["schedules"].Value,
                @"<option[^>]*value=[""']?(?<scheduleId>\d+)[""']?[^>]*>(?!.*?\b(Playoff|Elimination)s?\b).*?</option>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .OrderBy(m => Regex.Match(m.Groups[0].Value, @"<option[^>]*\bselected\b[^>]>").Success)
                .ThenBy(m => int.Parse(m.Groups["scheduleId"].Value))
            .TakeLast(1)
            .Select(m => int.Parse(m.Groups["scheduleId"].Value))
            .Cast<int?>()
            .FirstOrDefault();

            var playoffScheduleId = Regex.Matches(
                Regex.Match(html,
                    @"<select[^>]*\bfilter_schedule\b[^>]*>(?<schedules>.*?)</select>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["schedules"].Value,
                @"<option[^>]*value=[""']?(?<scheduleId>\d+)[""']?[^>]*>(?=.*?\b(Playoff|Elimination)s?\b).*?</option>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .OrderBy(m => Regex.Match(m.Groups[0].Value, @"<option[^>]*\bselected\b[^>]>").Success)
                .ThenBy(m => int.Parse(m.Groups["scheduleId"].Value))
            .TakeLast(1)
            .Select(m => int.Parse(m.Groups["scheduleId"].Value))
            .Cast<int?>()
            .FirstOrDefault();

            if (playoffScheduleId < scheduleId)
                playoffScheduleId = null;

            var leagueId = Regex.Split(id.InnerText.Trim(), @"/+")[3] ?? leagueInfo.LeagueId;
            var playoffEndpoint = Regex.Match(html, @$"/vghlleagues/{leagueId}(?<playoffEndpoint>/(playoffs|elimination-games|playofflist))", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["playoffEndpoint"].Value;
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
                Info = leagueInfo with
                {
                    Features = features,
                    LeagueId = leagueId,
                    SeasonId = seasonId ?? leagueInfo.SeasonId,
                    ScheduleId = scheduleId ?? leagueInfo.ScheduleId,
                    PlayoffScheduleId = playoffScheduleId,
                    PlayoffEndpoint = string.IsNullOrWhiteSpace(playoffEndpoint) ? null : playoffEndpoint,
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
                @"<div[^>]*\b(?<type>irs|inactives|signings|callup_senddown|drops)\b[^>]*>.*?<tbody[^>]*>(?<records>.*?)</tbody>\s*</table>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var type = m.Groups["type"].Value.Trim();

                return type.ToUpper() switch
                {
                    "IRS" => Regex.Matches(m.Groups["records"].Value,
                            @"<td[^>]*>\s*<a[^>]*rosters\?id=(?<teamId>\d+)[^>]*>.*?</a>\s*</td>\s*<td[^>]*>\s*(?<action>Placed|Removed)\s*<a[^>]*player&(?:amp;)?id=(?<playerId>\d+)[^>]*>(?<playerName>.*?)</a>\s*.*?\s+from\s+injured\s+reserved\s*</td>\s*<td[^>]*>(?<timestamp>.*?)</td>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                        .Cast<Match>()
                        .Select(m => new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { m.Groups["teamId"].Value },
                            PlayerIds = new string[] { m.Groups["playerId"].Value },
                            PlayerNames = new string[] { m.Groups["playerName"].Value.Trim() },
                            Type = m.Groups["action"].Value.ToLower().Contains("placed")
                                ? RosterTransactionType.PlacedOnIr
                                : RosterTransactionType.RemovedFromIr,
                            Timestamp = ISiteApi.ParseDateTime(m.Groups["timestamp"].Value, TimeZoneInfo.Utc),
                        }
                        )
                        .Cast<RosterTransaction>(),

                    "INACTIVES" => Regex.Matches(m.Groups["records"].Value,
                            @"<td[^>]*>\s*<a[^>]*player&(?:amp;)?id=(?<playerId>\d+)[^>]*>(?<playerName>.*?)</a>\s*</td>\s*<td[^>]*>\s*Has\s+been\s+reported\s+inactive.*?<a[^>]*rosters\?id=(?<teamId>\d+)[^>]*>.*?</a>.*?</td>\s*<td[^>]*>(?<timestamp>.*?)</td>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                        .Cast<Match>()
                        .Select(m => new RosterTransaction
                        {
                            LeagueId = league.Id,
                            TeamIds = new string[] { m.Groups["teamId"].Value },
                            PlayerIds = new string[] { m.Groups["playerId"].Value },
                            PlayerNames = new string[] { m.Groups["playerName"].Value.Trim() },
                            Type = RosterTransactionType.ReportedInactive,
                            Timestamp = ISiteApi.ParseDateTime(m.Groups["timestamp"].Value, TimeZoneInfo.Utc),
                        })
                        .Cast<RosterTransaction>(),

                    "SIGNINGS" => Regex.Matches(m.Groups["records"].Value,
                            @"<td[^>]*>\s*<img[^>]*/(?<teamAbbrev>\w+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>(?<playerName>.*?)\s*(?<playerPosition>\S+/\S+)\s*has\s+been\s+auto\s+assigned\s+a\s+Practice\s+Roster\s+contract\s+with\s+.*?</td>\s*<td[^>]*>(?<timestamp>.*?)</td>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                        .Cast<Match>()
                        .Select(m =>
                        {
                            if (!lookup.ContainsKey(m.Groups["teamAbbrev"].Value.Trim()))
                                return null;

                            return new RosterTransaction
                            {
                                LeagueId = league.Id,
                                TeamIds = new string[] { lookup[m.Groups["teamAbbrev"].Value.Trim()] },
                                PlayerNames = new string[] { m.Groups["playerName"].Value.Trim() },
                                Type = RosterTransactionType.AssignedToPracticeRoster,
                                Timestamp = ISiteApi.ParseDateTime(m.Groups["timestamp"].Value, TimeZoneInfo.Utc),
                            };
                        })
                        .Cast<RosterTransaction>(),

                    "CALLUP_SENDDOWN" => Regex.Matches(m.Groups["records"].Value,
                            @"<td[^>]*>\s*<img[^>]*/(?<fromTeamAbbrev>\w+)\.\w{3,4}[^>]*>\s*<i[^>]*>\s*</i>\s*<img[^>]*/(?<toTeamAbbrev>\w+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>.*?have\s*(?<action>called up|sent down)\s*(?<playerName>.*?)\s*(?<playerPosition>\S+/\S+)\s*(?<playerContract>.*?)\s*(?:from|to)\s*.*?</td>\s*<td[^>]*>(?<timestamp>.*?)</td>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                        .Cast<Match>()
                        .Select(m =>
                        {
                            if (!lookup.ContainsKey(m.Groups["fromTeamAbbrev"].Value.Trim()) || !lookup.ContainsKey(m.Groups["toTeamAbbrev"].Value.Trim()))
                                return null;

                            return new RosterTransaction
                            {
                                LeagueId = league.Id,
                                TeamIds = new string[] { lookup[m.Groups["fromTeamAbbrev"].Value.Trim()], lookup[m.Groups["toTeamAbbrev"].Value.Trim()] },
                                PlayerNames = new string[] { m.Groups["playerName"].Value.Trim() },
                                Type = m.Groups["action"].Value.ToLower().Contains("called up")
                                ? RosterTransactionType.CalledUp
                                : RosterTransactionType.SentDown,
                                Timestamp = ISiteApi.ParseDateTime(m.Groups["timestamp"].Value, TimeZoneInfo.Utc),
                            };
                        })
                        .Cast<RosterTransaction>(),

                    "DROPS" => Regex.Matches(m.Groups["records"].Value,
                            @"<td[^>]*>\s*<img[^>]*/(?<teamAbbrev>\w+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>.*?dropped\s*(?<playerName>.*?)\s*(?<playerPosition>\S+/\S+)\s*(?<playerContract>.*?)\s*.*?</td>\s*<td[^>]*>(?<timestamp>.*?)</td>",
                            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                        .Cast<Match>()
                        .Select(m =>
                        {
                            if (!lookup.ContainsKey(m.Groups["teamAbbrev"].Value.Trim()))
                                return null;

                            return new RosterTransaction
                            {
                                LeagueId = league.Id,
                                TeamIds = new string[] { lookup[m.Groups["teamAbbrev"].Value.Trim()] },
                                PlayerNames = new string[] { m.Groups["playerName"].Value.Trim() },
                                Type = Regex.Match(m.Groups[0].Value, @"\bBL\d*\b").Success
                                ? RosterTransactionType.Banned
                                : RosterTransactionType.Dropped,
                                Timestamp = ISiteApi.ParseDateTime(m.Groups["timestamp"].Value, TimeZoneInfo.Utc),
                            };
                        })
                        .Cast<RosterTransaction>(),

                    _ => new List<RosterTransaction>()
                };
            })
            .SelectMany(r => r)
            .Where(r => r != null)
            .Reverse()
            .ToList();
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
                    @"<select[^>]*\bfilter_stat_team\b[^>]*>(?<teams>.*?)</select>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups["teams"].Value,
                @"<option[^>]*value=[""']?(?<teamId>\d+)[""']?[^>]*>(?<teamName>.*?)</option>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (nameMatches.Count() == 0)
                return null;

            var lookup = await GetTeamLookupAsync(league);

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

            html = await _httpClient.GetStringAsync(GetUrl(league,
                path: "schedule",
                parameters: new Dictionary<string, object?>
                {
                    ["single_seasons"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
                }));

            var shortNameMatches = Regex.Matches(html,
                @"<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(?<teamAbbrev>\w+)\.\w{3,4}[^>]*>\s*</div>\s*<div[^>]*\bschedule-team\b[^>]*>\s*<div[^>]*\bschedule-team-name\b[^>]*>(?<teamName>.*?)</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .DistinctBy(m => m.Groups["teamAbbrev"].Value);

            foreach (var match in shortNameMatches)
            {
                if (!lookup.ContainsKey(match.Groups["teamAbbrev"].Value))
                    continue;

                var id = lookup[match.Groups["teamAbbrev"].Value];

                if (!teams.ContainsKey(id))
                    teams.Add(id, new Team { LeagueId = league.Id, Id = id });

                teams[id].ShortName = match.Groups["teamName"].Value.Trim();

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
        var nhlPredators = teams.FirstOrDefault(t => t.Name == "Nashville Nashville");

        if (nhlPredators != null)
        {
            nhlPredators.Name = "Nashville Predators";
            nhlPredators.ShortName = "Predators";
        }

        var ahlSenators = teams.FirstOrDefault(t => t.Name == "Bellevile Senators");

        if (ahlSenators != null)
        {
            ahlSenators.Name = "Belleville Senators";
            ahlSenators.ShortName = "Senators";
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
                @"<div[^>]*\bTrades\b[^>]*>.*?<tbody[^>]*>(?<records>.*?)</tbody>\s*</table>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!trades.Success)
                return new List<Trade>();

            var lookup = await GetTeamLookupAsync(league);

            return Regex.Matches(trades.Groups["records"].Value,
                @"<td[^>]*>\s*<img[^>]*/(?<fromTeamAbbrev>\w+)\.\w{3,4}[^>]*>\s*<i[^>]*>\s*</i>\s*<img[^>]*/(?<toTeamAbbrev>\w+)\.\w{3,4}[^>]*>\s*</td>\s*<td[^>]*>(?<trade>.*?)</td>\s*<td[^>]*>(?<timestamp>.*?)</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                var trade = Regex.Match(m.Groups["trade"].Value, @"The .*? have traded (?<playerName>.*?)\s*(?:(?<playerPosition>\w+/\w+)\s*(?<playerContract>.*?\$[\d,.]+))?\s*to the .*?.", RegexOptions.IgnoreCase | RegexOptions.Singleline);

                if (!trade.Success || !lookup.ContainsKey(m.Groups["fromTeamAbbrev"].Value.Trim()) || !lookup.ContainsKey(m.Groups["toTeamAbbrev"].Value.Trim()))
                    return null;

                return new Trade
                {
                    LeagueId = league.Id,
                    FromId = lookup[m.Groups["fromTeamAbbrev"].Value.Trim()],
                    ToId = lookup[m.Groups["toTeamAbbrev"].Value.Trim()],
                    FromAssets = new string[] { Regex.Replace(trade.Groups["playerName"].Value.Trim(), @"the (.*? \d+\S+) round draft pick", @"$1 Round Pick") },
                    Timestamp = ISiteApi.ParseDateTime(m.Groups["timestamp"].Value, TimeZoneInfo.Utc),
                };
            })
            .Where(t => t != null)
            .Cast<Trade>()
            .Reverse()
            .ToList();
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

    private async Task<IEnumerable<Game>> GetPlayoffGamesAsync(League league, IDictionary<string, string>? teams = null)
    {
        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        if (string.IsNullOrWhiteSpace(leagueInfo.PlayoffEndpoint))
            return new List<Game>();

        var html = await _httpClient.GetStringAsync(GetUrl(league,
            path: leagueInfo.PlayoffEndpoint), removeComments: true);

        var series = Regex.Matches(html,
            @$"<a[^>]*href=""(?<seriesUrl>(?=[^""]+\bview=playoffseries\b)(?=[^""]+\bschedule={leagueInfo.ScheduleId}\b)[^""]+)[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<img[^>]*/(?<topTeamAbbrev>\w+)\.\w{{3,4}}[^>]*>\s*</div>\s*<div[^>]*>\s*<span[^>]*>.*?</span>\s*<br[^>]*>\s*<span[^>]*club-name-header[^>]*>(?<topTeamName>.*?)</span>\s*</div>\s*</div>\s*<div[^>]*>\s*<div[^>]*>\s*<img[^>]*/(?<bottomTeamAbbrev>\w+)\.\w{{3,4}}[^>]*>\s*</div>\s*<div[^>]*>\s*<span[^>]*>.*?</span>\s*<br[^>]*>\s*<span[^>]*club-name-header[^>]*>(?<bottomTeamName>.*?)</span>\s*</div>\s*</div>\s*</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (series.Count() == 0)
            return new List<Game>();

        teams ??= await GetTeamLookupAsync(league);

        return (await Task.WhenAll(series.Select(async s =>
        {
            var _teams = new Dictionary<string, string>() {
                { s.Groups["topTeamName"].Value, teams[s.Groups["topTeamAbbrev"].Value] },
                { s.Groups["bottomTeamName"].Value, teams[s.Groups["bottomTeamAbbrev"].Value] },
            };

            var _html = await _httpClient.GetStringAsync(GetUrl(league,
                path: Regex.Replace(s.Groups["seriesUrl"].Value, @$"^(https://{Domain})?/?vghlleagues/{leagueInfo.LeagueId}", "")));

            return Regex.Matches(_html,
                @"<tr[^>]*playoffseriesRow[^>]*>\s*<td[^>]*>\s*<span[^>]*>\s*(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s*</span>\s*</td>\s*<td[^>]*>\s*<span[^>]*>\s*(?<visitorTeamName>.*?)\s*</span>\s*</td>\s*<td[^>]*>\s*<span[^>]*>\s*(?<homeTeamName>.*?)\s*</span>\s*</td>\s*<td[^>]*>\s*<div[^>]*>\s*<a[^>]*id=(?<gameId>\d+)[^>]*>\s*(?:Edit|(?<visitorScore>\d+)\s*-\s*(?<homeScore>\d+)\s*(?:\((?<period>\w+)\))?)\s*</a>\s*</div>\s*</td>\s*</tr>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                if (!_teams.TryGetValue(m.Groups["visitorTeamName"].Value.Trim(), out var visitorId) || !_teams.TryGetValue(m.Groups["homeTeamName"].Value.Trim(), out var homeId))
                    return null;

                return new Game
                {
                    LeagueId = league.Id,
                    Id = ulong.Parse(m.Groups["gameId"].Value),
                    Timestamp = ISiteApi.ParseDateTime(m.Groups["timestamp"].Value, Timezone),
                    VisitorId = visitorId!,
                    VisitorScore = int.TryParse(m.Groups["visitorScore"].Value, out var visitorScore) ? visitorScore : null,
                    HomeId = homeId,
                    HomeScore = int.TryParse(m.Groups["homeScore"].Value, out var homeScore) ? homeScore : null,
                    Overtime = m.Groups["period"].Value.ToUpper().Contains("OT"),
                    Shootout = m.Groups["period"].Value.ToUpper().Contains("SO"),
                };
            })
            .Where(g => g != null)
            .Cast<Game>();
        })))
        .SelectMany(g => g)
        .ToList();
    }

    private async Task<IEnumerable<Game>> GetRegularSeasonGamesAsync(League league, IDictionary<string, string>? teams = null)
    {
        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(league,
            path: "schedule",
            parameters: new Dictionary<string, object?>
            {
                ["single_seasons"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
            }));

        var weeks = Regex.Matches(html,
            @"<option[^>]*value=[""']?(?<week>\d{8})[""']?[^>]*>\d{4}-\d{2}-\d{2}</option>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
                .Select(m => m.Groups["week"].Value);

        if (weeks.Count() == 0)
            return new List<Game>();

        teams ??= await GetTeamLookupAsync(league);

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
                    @"(?<timestamp>\d+.{2} \S+ \d{4} @ \d+:\d+[ap]m)",
                    @"<div[^>]*\bgame_div_(?<gameId>\d+)\b[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(?<visitorId>\w+)\.\w{3,4}[^>]*>\s*</div>\s*<div[^>]*>.*?</div>\s*<div[^>]*\bschedule-team-score\b[^>]*>\s*(?<visitorScore>\d+|-)\s*</div>\s*</div>\s*<div[^>]*>\s*<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(?<homeId>\w+)\.\w{3,4}[^>]*>\s*</div>\s*<div[^>]*>.*?</div>\s*<div[^>]*\bschedule-team-score\b[^>]*>\s*(?<homeScore>\d+|-)\s*</div>\s*</div>\s*<div[^>]*>.*?</div>\s*</div>\s*<div[^>]*\bschedule-summary-link\b[^>]*>\s*<a[^>]*>(?<status>Final|Stats)(?:/(?<period>OT|SO))?</a>\s*</div>")})",
                RegexOptions.IgnoreCase | RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m =>
            {
                if (!string.IsNullOrWhiteSpace(m.Groups["timestamp"].Value))
                {
                    date = ISiteApi.ParseDateTime(Regex.Replace(m.Groups["timestamp"].Value, @"^(\d+).{2} (\S+) (\d+) @ (.*?)", @"$2 $1, $3 $4"), Timezone);
                    return null;
                }

                if (date == null)
                    return null;

                return new Game
                {
                    LeagueId = league.Id,
                    Id = ulong.Parse(m.Groups["gameId"].Value),
                    Timestamp = date.GetValueOrDefault(),
                    VisitorId = teams[m.Groups["visitorId"].Value.Trim()],
                    VisitorScore = int.TryParse(m.Groups["visitorScore"].Value, out var visitorScore) ? visitorScore : null,
                    HomeId = teams[m.Groups["homeId"].Value.Trim()],
                    HomeScore = int.TryParse(m.Groups["homeScore"].Value, out var homeScore) ? homeScore : null,
                    Overtime = m.Groups["period"].Value.ToUpper().Contains("OT"),
                    Shootout = m.Groups["period"].Value.ToUpper().Contains("SO"),
                };
            })
            .Where(g => g != null)
            .Cast<Game>();
        })))
        .SelectMany(g => g)
        .ToList();
    }

    private async Task<IDictionary<string, string>> GetTeamLookupAsync(League league, bool includeAffiliates = false)
    {
        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(league,
            path: "standings",
            parameters: new Dictionary<string, object?>
            {
                ["filter_schedule"] = leagueInfo.ScheduleId > 0 ? leagueInfo.ScheduleId : null,
            }));

        if (includeAffiliates && (league.Affiliates?.Count() ?? 0) > 0)
        {
            html += string.Join("", await Task.WhenAll(
                league.Affiliates!.Select(affiliate =>
                {
                    var affiliateInfo = (affiliate.Affiliate.Info as MyVirtualGamingLeagueInfo)!;
                    return _httpClient.GetStringAsync($"https://{Domain}/vghlleagues/{affiliateInfo.LeagueId}/standings");
                })));
        }

        var lookup = Regex.Matches(html,
            @"<a[^>]*/rosters\?id=(?<teamId>\d+)[^>]*>\s*<img[^>]*/(?<teamAbbrev>\w+)\.\w{3,4}[^>]*>\s*<\/a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .Cast<Match>()
        .DistinctBy(m => m.Groups["teamAbbrev"].Value.ToUpper())
        .ToDictionary(
            m => m.Groups["teamAbbrev"].Value.ToUpper(),
            m => m.Groups["teamId"].Value,
            StringComparer.OrdinalIgnoreCase);

        if (lookup.ContainsKey("TAP") && !lookup.ContainsKey("TAPP"))
            lookup.Add("TAPP", lookup["TAP"]);

        if (!lookup.ContainsKey("TAP") && lookup.ContainsKey("TAPP"))
            lookup.Add("TAP", lookup["TAPP"]);

        return lookup;
    }
}