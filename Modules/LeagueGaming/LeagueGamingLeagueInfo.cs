namespace Duthie.Modules.LeagueGaming;

internal class LeagueGamingLeagueInfo
{
    public int LeagueId { get; set; }
    public int SeasonId { get; set; }
    public int ForumId { get; set; }
    public string? LogoUrl => $"https://www.leaguegaming.com/images/league/icon/l{LeagueId}_100.png";
}