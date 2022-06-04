namespace Duthie.Types.Common;

public class HashHistory : HashSet<ulong>
{
    public HashHistory() : base(5) { }

    public HashHistory(IEqualityComparer<ulong>? comparer) : base(5, comparer) { }
}