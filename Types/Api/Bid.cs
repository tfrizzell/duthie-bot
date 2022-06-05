using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

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

    public string GetHash()
    {
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                LeagueId,
                TeamExternalId,
                Player = string.IsNullOrWhiteSpace(PlayerExternalId) ? PlayerName : PlayerExternalId,
                Amount,
                State,
                Timestamp
            })));

            return BitConverter.ToString(hash);
        }
    }

    public override int GetHashCode() =>
        HashCode.Combine(
            GetType().GetHashCode(),
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