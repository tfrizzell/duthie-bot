using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Duthie.Data.Comparers;

public class GuidValueComparer : ValueComparer<Guid>
{
    public GuidValueComparer()
        : base(
            (g1, g2) => g1.ToString().ToLower().Equals(g2.ToString().ToLower()),
            g => g.GetHashCode())
    { }
}