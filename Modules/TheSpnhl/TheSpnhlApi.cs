using System.Text.RegularExpressions;
using System.Web;
using Duthie.Types;
using Duthie.Types.Api;

namespace Duthie.Modules.TheSpnhl;

public class TheSpnhlApi : ILeagueInfoApi, ITeamsApi
{
    private readonly HttpClient _httpClient;

    public TheSpnhlApi()
    {
        _httpClient = new HttpClient();
    }

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { TheSpnhlSiteProvider.SITE_ID };
    }

    private string GetUrl(IDictionary<string, object> parameters, string file = "index.php", string path = "leaguegaming/league")
    {
        var queryString = string.Join("&", parameters.Select(p => string.Join("=", HttpUtility.UrlEncode(p.Key), HttpUtility.UrlEncode(p.Value.ToString()))));
        return $"https://www.leaguegaming.com/forums/{file}?{path}&{queryString}".Replace("?&", "?");
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

        var teams = new Dictionary<string, LeagueTeam>();

        foreach (Match match in matches)
        {
            var id = match.Groups[1].Value.Trim();

            if (teams.ContainsKey(id))
                continue;

            var team = DefaultTeams.GetByAbbreviation(id, leagueInfo.LeagueType);

            if (team != null)
            {
                teams.Add(id, new LeagueTeam
                {
                    LeagueId = league.Id,
                    League = league,
                    TeamId = team.Id,
                    Team = team,
                    IId = id
                });
            }
        }

        return teams.Values.ToList();
    }
}