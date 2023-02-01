namespace Duthie.Modules.Leaguegaming;

internal record LeaguegamingLeagueInfo
{
    public int LeagueId { get; set; }
    public int SeasonId { get; set; }
    public int ForumId { get; set; }
    public int? DraftId { get; set; }
    public DateTimeOffset? DraftDate { get; set; }
}