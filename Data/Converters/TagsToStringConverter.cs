using System.Text.Json;
using Duthie.Types.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Duthie.Data.Converters;

public class TagsToStringConverter : ValueConverter<Tags, string>
{
    public TagsToStringConverter()
        : base(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<Tags>(value, (JsonSerializerOptions?)null) ?? new Tags())
    { }
}