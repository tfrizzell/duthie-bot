using System.Text.Json;
using Duthie.Types.Guilds;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Duthie.Data.Converters;

public class GuildMessageEmbedToStringConverter : ValueConverter<GuildMessageEmbed?, string?>
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public GuildMessageEmbedToStringConverter()
        : base(
            value => value == null ? null : JsonSerializer.Serialize(value, JsonOptions),
            value => value == null ? null : JsonSerializer.Deserialize<GuildMessageEmbed>(value, JsonOptions))
    { }
}