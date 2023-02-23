namespace Duthie.Types.Leagues;

public class LeagueState
{
    public Guid LeagueId { get; set; }
    public string? LastBidHash { get; set; }
    public DateTimeOffset? LastBidTimestamp { get; set; }
    public string? LastContractHash { get; set; }
    public DateTimeOffset? LastContractTimestamp { get; set; }
    public DateTimeOffset? LastDailyStarTimestamp { get; set; }
    public string? LastDraftPickHash { get; set; }
    public DateTimeOffset? LastDraftPickTimestamp { get; set; }
    public string? LastNewsItemHash { get; set; }
    public DateTimeOffset? LastNewsItemTimestamp { get; set; }
    public string? LastRosterTransactionHash { get; set; }
    public DateTimeOffset? LastRosterTransactionTimestamp { get; set; }
    public string? LastTradeHash { get; set; }
    public DateTimeOffset? LastTradeTimestamp { get; set; }
    public string? LastWaiverHash { get; set; }
    public DateTimeOffset? LastWaiverTimestamp { get; set; }

#nullable disable
    internal virtual League League { get; set; }
#nullable enable
}