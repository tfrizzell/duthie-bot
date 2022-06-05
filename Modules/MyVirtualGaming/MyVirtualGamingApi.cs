using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using Duthie.Types.Api;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;

namespace Duthie.Modules.MyVirtualGaming;

public class MyVirtualGamingApi
    : IBidApi, IGameApi, ILeagueInfoApi, ITeamApi
{
    private static readonly TimeZoneInfo Timezone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    private readonly HttpClient _httpClient = new HttpClient();

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { MyVirtualGamingSiteProvider.MyVirtualGaming.Id };
    }

    private string GetUrl(string league = "vghl", string path = "", IDictionary<string, object?>? parameters = null)
    {
        var queryString = parameters == null ? string.Empty
            : string.Join("&", parameters.Where(p => p.Value != null).Select(p => string.Join("=", HttpUtility.UrlEncode(p.Key), HttpUtility.UrlEncode(p.Value!.ToString()))));
        return Regex.Replace($"https://vghl.myvirtualgaming.com/vghlleagues/{league}/{path}?{queryString}".Replace("?&", "?"), @"[?&]+$", "");
    }

    private bool IsSupported(League league) =>
        Supports.Contains(league.SiteId) || league.Info is MyVirtualGamingLeagueInfo;

    public async Task<IEnumerable<Bid>?> GetBidsAsync(League league)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        if (!leagueInfo.Features.HasFlag(MyVirtualGamingFeatures.RecentTransactions))
            return new List<Bid>();

        var html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "recent-transactions"));

        var closedBids = Regex.Match(html,
            @"<div[^>]*\bclosed-bids\b[^>]*>.*?<tbody[^>]*\btransaction-list\b[^>]*>(.*?)</tbody>\s*</table>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!closedBids.Success)
            return new List<Bid>();

        return Regex.Matches(closedBids.Groups[1].Value,
            @"<td[^>]*\bmvg-col-trans-team-name\b[^>]*>\s*<a[^>]*id=(\d+)[^>]*>\s*<img[^>]*>.*?</a>\s*</td>\s*<td[^>]*\bmvg-col-transaction-detail\b[^>]*>(.*?)</td>\s*<td[^>]*>.*?</td>\s*<td[^>]*>.*?</td>\s*<td[^>]*\bmvg-col-trans-datetime\b[^>]*>(.*?)</td>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .Select(m =>
        {
            var dateTime = DateTime.Parse(m.Groups[3].Value.Trim());
            var player = Regex.Match(m.Groups[2].Value, @"<a[^>]*player&id=(\d+)[^>]*>(.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            return new Bid
            {
                LeagueId = league.Id,
                TeamExternalId = m.Groups[1].Value.Trim(),
                PlayerExternalId = player.Groups[1].Value.Trim(),
                PlayerName = player.Groups[2].Value.Trim(),
                Amount = ISiteApi.ParseDollars(Regex.Match(m.Groups[2].Value, @"\$[\d\.]+( \w)?", RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[0].Value),
                State = BidState.Won,
                Timestamp = new DateTimeOffset(dateTime, Timezone.GetUtcOffset(dateTime)),
            };
        });
    }

    public async Task<IEnumerable<Game>?> GetGamesAsync(League league)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
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
            var _html = await _httpClient.GetStringAsync(GetUrl(
                league: leagueInfo.LeagueId,
                path: "schedule",
                parameters: new Dictionary<string, object?>
                {
                    ["single_seasons"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
                    ["filter_scheduled_week"] = week,
                }));

            var matches = Regex.Matches(_html,
                @$"(?:{string.Join("|",
                    @"(\d+.{2} \S+ \d{4} @ \d+:\d+[ap]m)",
                    @"<div[^>]*\bgame_div_(\d+)\b[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*>\s*<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(\w+)\.png[^>]*>\s*</div>\s*<div[^>]*>.*?</div>\s*<div[^>]*\bschedule-team-score\b[^>]*>\s*(\d+|-)\s*</div>\s*</div>\s*<div[^>]*>\s*<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(\w+)\.png[^>]*>\s*</div>\s*<div[^>]*>.*?</div>\s*<div[^>]*\bschedule-team-score\b[^>]*>\s*(\d+|-)\s*</div>\s*</div>\s*<div[^>]*>.*?</div>\s*</div>\s*<div[^>]*\bschedule-summary-link\b[^>]*>\s*<a[^>]*>(Final|Stats)(?:/(OT|SO))?</a>\s*</div>")})",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (matches.Count() == 0)
                return new List<Game>();

            DateTimeOffset? date = null;
            var games = new List<Game>();

            foreach (Match match in matches)
            {
                if (!string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    var dateTime = DateTime.Parse(Regex.Replace(match.Groups[1].Value, @"^(\d+).{2} (\S+) (\d+) @ (.*?)", @"$2 $1, $3 $4"));
                    date = new DateTimeOffset(dateTime, Timezone.GetUtcOffset(dateTime));
                    continue;
                }

                if (date == null)
                    continue;

                games.Add(new Game
                {
                    LeagueId = league.Id,
                    GameId = ulong.Parse(match.Groups[2].Value.Trim()),
                    Timestamp = date.GetValueOrDefault(),
                    VisitorExternalId = teams[match.Groups[3].Value.Trim()],
                    VisitorScore = int.TryParse(match.Groups[4].Value, out var visitorScore) ? visitorScore : null,
                    HomeExternalId = teams[match.Groups[5].Value.Trim()],
                    HomeScore = int.TryParse(match.Groups[6].Value, out var homeScore) ? homeScore : null,
                    Overtime = match.Groups[8].Value.ToUpper().Contains("OT"),
                    Shootout = match.Groups[8].Value.ToUpper().Contains("SO"),
                });
            }

            return games;
        }))).SelectMany(g => g);
    }

    public async Task<ILeague?> GetLeagueInfoAsync(League league)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        var xml = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
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

        var html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "schedule"));

        var seasonId = Regex.Matches(
            Regex.Match(html,
                @"<select[^>]*\bsingle_seasons\b[^>]*>(.*?)</select>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value,
            @"<option[^>]*value=[""']?(\d+)[""']?[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .OrderBy(m => Regex.Match(m.Groups[0].Value, @"<option[^>]*\bselected\b[^>]>").Success)
            .ThenBy(m => int.Parse(m.Groups[1].Value))
        .TakeLast(1)
        .Select(m => int.Parse(m.Groups[1].Value))
        .Cast<int?>()
        .FirstOrDefault();

        html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "standings"));

        var scheduleId = Regex.Matches(
            Regex.Match(html,
                @"<select[^>]*\bfilter_schedule\b[^>]*>(.*?)</select>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline).Groups[1].Value,
            @"<option[^>]*value=[""']?(\d+)[""']?[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .OrderBy(m => Regex.Match(m.Groups[0].Value, @"<option[^>]*\bselected\b[^>]>").Success)
            .ThenBy(m => int.Parse(m.Groups[1].Value))
        .TakeLast(1)
        .Select(m => int.Parse(m.Groups[1].Value))
        .Cast<int?>()
        .FirstOrDefault();

        return new League
        {
            Name = Regex.Replace(title.InnerText.Trim(), @"\s+Home$", ""),
            Info = new MyVirtualGamingLeagueInfo
            {
                LeagueId = Regex.Split(id.InnerText.Trim(), @"/+")[3] ?? leagueInfo.LeagueId,
                SeasonId = seasonId ?? leagueInfo.SeasonId,
                ScheduleId = scheduleId ?? leagueInfo.ScheduleId,
            }
        };
    }

    public async Task<IEnumerable<LeagueTeam>?> GetTeamsAsync(League league)
    {
        if (!IsSupported(league))
            return null;

        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
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

        html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "schedule",
            parameters: new Dictionary<string, object?>
            {
                ["single_seasons"] = leagueInfo.SeasonId > 0 ? leagueInfo.SeasonId : null,
            }));

        var shortNameMatches = Regex.Matches(html,
            @$"<div[^>]*\bschedule-team-logo\b[^>]*>\s*<img[^>]*/(\w+).png[^>]*>\s*</div>\s*<div[^>]*\bschedule-team\b[^>]*>\s*<div[^>]*\bschedule-team-name\b[^>]*>(.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .DistinctBy(m => m.Groups[1].Value);

        foreach (Match match in shortNameMatches)
        {
            if (!lookup.ContainsKey(match.Groups[1].Value))
                continue;

            var id = lookup[match.Groups[1].Value];

            if (!teams.ContainsKey(id))
                teams.Add(id, new LeagueTeam { LeagueId = league.Id, League = league, Team = new Team() });

            teams[id].Team.ShortName = match.Groups[2].Value.Trim();
            teams[id].ExternalId = id;

            if (string.IsNullOrWhiteSpace(teams[id].Team.Name))
                teams[id].Team.Name = teams[id].Team.ShortName;
        }

        return FixTeams(teams.Values
            .Where(t => lookup.Values.Contains(t.ExternalId))
            .ToList());
    }

    private async Task<IDictionary<string, string>> GetTeamLookupAsync(League league)
    {
        var leagueInfo = (league.Info as MyVirtualGamingLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(
            league: leagueInfo.LeagueId,
            path: "standings",
            parameters: new Dictionary<string, object?>
            {
                ["filter_schedule"] = leagueInfo.ScheduleId > 0 ? leagueInfo.ScheduleId : null,
            }));

        return Regex.Matches(html,
            @"<a[^>]*/rosters\?id=(\d+)[^>]*>\s*<img[^>]*/(\w+)\.\w{3,4}[^>]*>\s*<\/a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline)
        .DistinctBy(m => m.Groups[2].Value.ToUpper())
        .ToDictionary(
            m => m.Groups[2].Value.ToUpper(),
            m => m.Groups[1].Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private IEnumerable<LeagueTeam> FixTeams(List<LeagueTeam> teams)
    {
        var league = teams.FirstOrDefault()?.League;

        if (league?.Id == MyVirtualGamingLeagueProvider.VGNHL.Id)
        {
            teams.AddRange(
                teams.Where(t => t.Team.Name == "Nashville Nashville")
                    .ToList()
                    .Select(t => new LeagueTeam
                    {
                        LeagueId = t.LeagueId,
                        League = t.League,
                        Team = new Team
                        {
                            Name = "Nashville Predators",
                            ShortName = "Predators"
                        },
                        ExternalId = t.ExternalId,
                    })
            );
        }
        else if (league?.Id == MyVirtualGamingLeagueProvider.VGAHL.Id)
        {
            teams.AddRange(
                teams.Where(t => t.Team.Name == "Bellevile Senators")
                    .ToList()
                    .Select(t => new LeagueTeam
                    {
                        LeagueId = t.LeagueId,
                        League = t.League,
                        Team = new Team
                        {
                            Name = "Belleville Senators",
                            ShortName = "Senators"
                        },
                        ExternalId = t.ExternalId,
                    })
            );
        }

        return teams;
    }
}