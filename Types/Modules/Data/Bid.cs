using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Modules.Data;

public class Bid : IModuleData
{
    public Guid LeagueId { get; set; }
    public string TeamId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public ulong Amount { get; set; }
    public BidState State { get; set; }
    public DateTimeOffset? Timestamp { get; set; }

    public string GetHash()
    {
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                LeagueId,
                TeamId,
                Player = string.IsNullOrWhiteSpace(PlayerId) ? PlayerName : PlayerId,
                Amount,
                State,
                Timestamp,
            })));

            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}

public enum BidState
{
    Active,
    Won
}