namespace Duthie.Modules.Leaguegaming;

internal class LeaguegamingLeagueInfo
{
    public int LeagueId { get; set; }
    public int SeasonId { get; set; }
    public int ForumId { get; set; }
    public int? DraftId { get; set; }
    public DateTimeOffset? DraftDate { get; set; }
    public int[] AffiliatedLeagueIds { get; set; } = new int[] { };
}