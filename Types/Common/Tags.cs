using System.Runtime.Serialization;

namespace Duthie.Types;

public class Tags : HashSet<string>
{
    public Tags() : base() { }

    public Tags(IEnumerable<string> collection) : base(collection) { }

    public Tags(IEqualityComparer<string>? comparer) : base(comparer) { }

    public Tags(int capacity) : base(capacity) { }

    public Tags(IEnumerable<string> collection, IEqualityComparer<string>? comparer) : base(collection, comparer) { }

    public Tags(int capacity, IEqualityComparer<string>? comparer) : base(capacity, comparer) { }

    protected Tags(SerializationInfo info, StreamingContext context) : base(info, context) { }
}