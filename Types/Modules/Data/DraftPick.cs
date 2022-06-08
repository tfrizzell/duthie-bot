using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Modules.Data;

public class DraftPick : IModuleData
{
    public Guid LeagueId { get; set; }
    public string TeamId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public int RoundNumber { get; set; }
    public int RoundPick { get; set; }
    public int OverallPick { get; set; }
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
                RoundNumber,
                RoundPick,
                OverallPick,
                Timestamp,
            })));

            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}