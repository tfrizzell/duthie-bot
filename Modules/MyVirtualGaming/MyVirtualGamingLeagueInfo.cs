namespace Duthie.Modules.MyVirtualGaming;

internal class MyVirtualGamingLeagueInfo
{
    public MyVirtualGamingFeatures Features { get; set; } = MyVirtualGamingFeatures.All;
    public string LeagueId { get; set; } = "";
    public int SeasonId { get; set; }
    public int ScheduleId { get; set; }
    public string[] AffiliatedLeagueIds { get; set; } = new string[] { };
}