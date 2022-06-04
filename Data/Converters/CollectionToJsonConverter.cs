using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Duthie.Data.Converters;

public class CollectionToJsonConverter<T, U> : ValueConverter<T, string>
    where T : class, ICollection<U>
{
    public CollectionToJsonConverter()
        : base(
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<T>(value, (JsonSerializerOptions?)null) ?? Activator.CreateInstance<T>())
    { }
}

public class StringCollectionToJsonConverter<T> : CollectionToJsonConverter<T, string>
    where T : class, ICollection<string>
{ }

public class UlongCollectionToJsonConverter<T> : CollectionToJsonConverter<T, ulong>
    where T : class, ICollection<ulong>
{ }