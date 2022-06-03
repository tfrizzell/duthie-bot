using Duthie.Types.Common;
using Duthie.Types.Leagues;

namespace Duthie.Types.Sites;

public class Site
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Tags Tags { get; set; } = new Tags();
    public bool Enabled { get; set; } = true;

#nullable disable
    public virtual IReadOnlyCollection<League> Leagues { get; set; }
#nullable enable
}