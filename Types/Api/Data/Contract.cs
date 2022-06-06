using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Api.Data;

public class Contract : IApiData
{
    public Guid LeagueId { get; set; }
    public string TeamExternalId { get; set; } = "";
    public string PlayerExternalId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public int Length { get; set; } = 1;
    public ulong Amount { get; set; }
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
                Timestamp,
            })));

            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}