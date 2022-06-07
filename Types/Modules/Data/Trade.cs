using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Modules.Data;

public class Trade : IModuleData
{
    public Guid LeagueId { get; set; }
    public string FromId { get; set; } = "";
    public string ToId { get; set; } = "";
    public string[] Assets { get; set; } = new string[] { };
    public DateTimeOffset Timestamp { get; set; }

    public string GetHash()
    {
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                LeagueId,
                FromId,
                Assets,
                ToId,
                Timestamp,
            })));

            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}