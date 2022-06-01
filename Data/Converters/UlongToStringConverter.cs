using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Duthie.Data.Converters;

public class UlongToStringConverter : ValueConverter<ulong, string>
{
    public UlongToStringConverter()
        : base(
            value => value.ToString(),
            value => ulong.Parse(value))
    { }
}