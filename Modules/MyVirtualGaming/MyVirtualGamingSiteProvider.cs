using Duthie.Types.Common;
using Duthie.Types.Sites;

namespace Duthie.Modules.MyVirtualGaming;

public class MyVirtualGamingSiteProvider : ISiteProvider
{
    internal static readonly Site VGHL = new Site
    {
        Id = new Guid("40a06d17-e48f-49f1-9184-7393f035322c"),
        Name = "VGHL",
        Url = "vghl.myvirtualgaming.com",
        Tags = new Tags { "psn", "ea nhl" },
        Enabled = true,
    };

    public IReadOnlyCollection<Site> Sites
    {
        get => new Site[] { VGHL };
    }
}