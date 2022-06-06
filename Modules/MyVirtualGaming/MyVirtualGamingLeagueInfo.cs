namespace Duthie.Modules.MyVirtualGaming;

internal class MyVirtualGamingLeagueInfo
{
    public string LeagueId { get; set; } = "";
    public int SeasonId { get; set; }
    public int ScheduleId { get; set; }
    public string? LogoUrl { get; set; }
    public MyVirtualGamingFeatures Features { get; set; } = MyVirtualGamingFeatures.All;
}