using System.Text.Json;
using Duthie.Types.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Duthie.Data.Converters;

public class TagsToJsonConverter : ValueConverter<Tags, string>
{
    public TagsToJsonConverter()
        : base(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<Tags>(value, (JsonSerializerOptions?)null) ?? new Tags())
    { }
}