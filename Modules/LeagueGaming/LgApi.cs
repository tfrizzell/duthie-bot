using System.Text.RegularExpressions;
using System.Web;
using Duthie.Types;
using Duthie.Types.Api;

namespace Duthie.Modules.LeagueGaming;

public class LgApi : ISiteApi
{
    private readonly HttpClient _httpClient;

    public LgApi()
    {
        _httpClient = new HttpClient();
    }

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { LgSiteProvider.SITE_ID };
    }

    private string GetUrl(IDictionary<string, object> parameters, string file = "index.php", string path = "leaguegaming/league")
    {
        var queryString = string.Join("&", parameters.Select(p => string.Join("=", HttpUtility.UrlEncode(p.Key), HttpUtility.UrlEncode(p.Value.ToString()))));
        return $"https://www.leaguegaming.com/forums/{file}?{path}&{queryString}".Replace("?&", "?");
    }

    public async Task<ILeague> GetLeagueInfoAsync(League league)
    {
        if (league.Info is not LgLeagueInfo)
            return league;

        var leagueInfo = (league.Info as LgLeagueInfo)!;

        var html = await _httpClient.GetStringAsync(GetUrl(new Dictionary<string, object>
        {
            ["action"] = "league",
            ["page"] = "standings",
            ["leagueid"] = leagueInfo.LeagueId,
            ["seasonid"] = 1
        }));

        var info = Regex.Match(html, @$"<li[^>]* custom-tab-{leagueInfo.LeagueId} [^>]*>\s*<a[^>]*/league\.(\d+)[^>]*>.*?<span[^>]*>(.*?)</span>\s*</a>", RegexOptions.IgnoreCase & RegexOptions.Singleline);

        if (!info.Success)
            return league;

        var season = Regex.Match(html, @$"<a[^>]*leagueid={leagueInfo.LeagueId}&(?:amp;)?seasonid=(\d+)[^>]*>Roster", RegexOptions.IgnoreCase & RegexOptions.Singleline);

        return new League
        {
            Name = info.Groups[2].Value.Trim(),
            Info = new LgLeagueInfo
            {
                LeagueId = leagueInfo.LeagueId,
                SeasonId = season.Success ? int.Parse(season.Groups[1].Value) : leagueInfo.SeasonId,
                ForumId = int.Parse(info.Groups[1].Value)
            }
        };
    }
}