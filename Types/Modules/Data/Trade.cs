using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Modules.Data;

public class Trade : IModuleData
{
    public Guid LeagueId { get; set; }
    public string FromId { get; set; } = "";
    public string ToId { get; set; } = "";
    public string[] FromAssets { get; set; } = new string[] { };
    public string[] ToAssets { get; set; } = new string[] { };
    public DateTimeOffset Timestamp { get; set; }

    public string GetHash()
    {
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                LeagueId,
                FromId,
                FromAssets,
                ToId,
                Timestamp,
            })));

            return BitConverter.ToString(hash).Replace("-", "");
        }
    }

    public void Reverse()
    {
        var fromId = FromId;
        FromId = ToId;
        ToId = fromId;

        var fromAssets = FromAssets;
        FromAssets = ToAssets;
        ToAssets = fromAssets;
    }
}