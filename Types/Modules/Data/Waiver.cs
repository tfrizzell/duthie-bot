using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Modules.Data;

public class Waiver : IModuleData
{
    public Guid LeagueId { get; set; }
    public string TeamId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public WaiverActionType Type { get; set; }
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
                Type,
                Timestamp,
            })));

            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}

public enum WaiverActionType
{
    Placed,
    Removed,
    Claimed,
    Cleared,
}