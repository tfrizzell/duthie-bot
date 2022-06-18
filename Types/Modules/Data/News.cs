using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Modules.Data;

public class News : IModuleData
{
    public Guid LeagueId { get; set; }
    public string TeamId { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTimeOffset? Timestamp { get; set; }

    public string GetHash()
    {
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                LeagueId,
                TeamId,
                Message,
                Timestamp,
            })));

            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}