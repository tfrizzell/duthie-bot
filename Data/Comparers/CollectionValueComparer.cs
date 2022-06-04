using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Duthie.Data.Comparers;

public class CollectionValueComparer<T, U> : ValueComparer<T>
    where T : class, ICollection<U>
{
    public CollectionValueComparer()
        : base(
            (t1, t2) => t1!.SequenceEqual(t2!),
            t => t.Aggregate(0, (a, v) => HashCode.Combine(a, v == null ? 0 : v.GetHashCode())))
    { }
}

public class StringCollectionValueComparer<T> : CollectionValueComparer<T, string>
    where T : class, ICollection<string>
{ }

public class UlongCollectionValueComparer<T> : CollectionValueComparer<T, ulong>
    where T : class, ICollection<ulong>
{ }