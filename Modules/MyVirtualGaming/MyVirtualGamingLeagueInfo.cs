namespace Duthie.Modules.MyVirtualGaming;

internal record MyVirtualGamingLeagueInfo
{
    public MyVirtualGamingFeatures Features { get; set; } = MyVirtualGamingFeatures.All;
    public string LeagueId { get; set; } = "";
    public int SeasonId { get; set; }
    public int ScheduleId { get; set; }
    public int? PlayoffScheduleId { get; set; }
    public string? PlayoffEndpoint { get; set; }
}