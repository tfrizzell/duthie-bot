namespace Duthie.Types.Api;

public class Bid
{
    public Guid LeagueId { get; set; }
    public string TeamExternalId { get; set; } = "";
    public string PlayerExternalId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public ulong Amount { get; set; }
    public BidState State { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public override int GetHashCode() =>
        HashCode.Combine(
            LeagueId.GetHashCode(),
            TeamExternalId.GetHashCode(),
            (string.IsNullOrWhiteSpace(PlayerExternalId) ? PlayerName : PlayerExternalId).GetHashCode(),
            Amount.GetHashCode(),
            State.GetHashCode(),
            Timestamp.GetHashCode());
}

public enum BidState
{
    Active,
    Won
}