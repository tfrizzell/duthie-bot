using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Modules.Data;

public class Game : IModuleData
{
    public Guid LeagueId { get; set; }
    public ulong Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string VisitorId { get; set; } = "";
    public int? VisitorScore { get; set; }
    public string HomeId { get; set; } = "";
    public int? HomeScore { get; set; }
    public bool? Overtime { get; set; }
    public bool? Shootout { get; set; }

    public string GetHash()
    {
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                LeagueId,
                Id,
                Timestamp,
                VisitorId,
                VisitorScore,
                HomeId,
                HomeScore,
                Overtime,
                Shootout,
            })));

            return BitConverter.ToString(hash);
        }
    }
}