using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Api.Types;

public class Game
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public ulong GameId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string VisitorExternalId { get; set; } = "";
    public int? VisitorScore { get; set; }
    public string HomeExternalId { get; set; } = "";
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
                GameId,
                Timestamp,
                VisitorExternalId,
                VisitorScore,
                HomeExternalId,
                HomeScore,
                Overtime,
                Shootout,
            })));

            return BitConverter.ToString(hash);
        }
    }
}