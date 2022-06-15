using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Duthie.Types.Modules.Data;

public class League : IModuleData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? LogoUrl { get; set; }
    public object? Info { get; set; }

    public string GetHash()
    {
        using (var sha1 = SHA1.Create())
        {
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                Id,
                Name,
                LogoUrl,
                Info,
            })));

            return BitConverter.ToString(hash);
        }
    }
}

public class League<T> : League
{
    public new T? Info { get; set; }
}