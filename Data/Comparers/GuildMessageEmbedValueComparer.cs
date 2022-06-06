using Duthie.Types.Guilds;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Duthie.Data.Comparers;

public class GuildMessageEmbedValueComparer : ValueComparer<GuildMessageEmbed?>
{
    public GuildMessageEmbedValueComparer()
        : base(
            (o1, o2) => o1 == null ? o2 == null : (o2 != null && o1.GetHashCode() == o2.GetHashCode()),
            o => o == null ? -1 : o.GetHashCode())
    { }
}