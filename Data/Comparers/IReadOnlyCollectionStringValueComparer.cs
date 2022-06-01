using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Duthie.Data.Comparers;

public class IReadOnlyCollectionStringValueComparer : ValueComparer<IReadOnlyCollection<string>>
{
    public IReadOnlyCollectionStringValueComparer()
        : base(
            (t1, t2) => t1!.SequenceEqual(t2!),
            t => t.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            t => t.ToArray())
    { }
}