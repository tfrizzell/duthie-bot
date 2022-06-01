using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Duthie.Data.Comparers;

public class UlongValueComparer : ValueComparer<ulong>
{
    public UlongValueComparer()
        : base(
            (t1, t2) => t1 == t2,
            t => t.GetHashCode())
    { }
}