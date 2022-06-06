using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Duthie.Data.Converters;

public class LeagueInfoToStringConverter : ValueConverter<object?, string?>
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LeagueInfoToStringConverter()
        : base(
            value => Serialize(value),
            value => Deserialize(value))
    { }

    private static string? Serialize(object? value)
    {
        if (value == null)
            return null;

        return JsonSerializer.Serialize(new LeagueInfo(value.GetType().ToString(), value), JsonOptions);
    }

    private static object? Deserialize(string? value)
    {
        if (value == null)
            return null;

        var info = JsonSerializer.Deserialize<LeagueInfo>(value, JsonOptions)!;
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.FullName == info.Type);

        if (type == null)
            return info.Data;

        return JsonSerializer.Deserialize(JsonSerializer.Serialize(info.Data, JsonOptions), type, JsonOptions);
    }

    private record LeagueInfo
    (
        string Type,
        object Data
    );
}