using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Modules.Data;

public class RosterTransaction : IModuleData
{
    public Guid LeagueId { get; set; }
    public string[] TeamIds { get; set; } = new string[] { };
    public string[] PlayerIds { get; set; } = new string[] { };
    public string[] PlayerNames { get; set; } = new string[] { };
    public RosterTransactionType Type { get; set; }
    public DateTimeOffset? Timestamp { get; set; }

    public string GetHash()
    {
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                LeagueId,
                TeamIds,
                Players = PlayerIds.Count() > 0 ? PlayerIds : PlayerNames,
                Type,
                Timestamp,
            })));

            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}

public enum RosterTransactionType
{
    PlacedOnIr,
    RemovedFromIr,
    ReportedInactive,
    CalledUp,
    SentDown,
    Banned,
    Suspended,
}