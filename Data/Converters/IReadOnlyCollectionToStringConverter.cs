using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Duthie.Data.Converters;

public class IReadOnlyCollectionToStringConverter : ValueConverter<IReadOnlyCollection<string>, string>
{
    public IReadOnlyCollectionToStringConverter()
        : base(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<string[]>(value, (JsonSerializerOptions?)null) ?? new string[] { })
    { }
}