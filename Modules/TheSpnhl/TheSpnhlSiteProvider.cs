using Duthie.Types.Common;
using Duthie.Types.Sites;

namespace Duthie.Modules.TheSpnhl;

public class TheSpnhlSiteProvider : ISiteProvider
{
    internal static readonly Site SPNHL = new Site
    {
        Id = new Guid("c193a2eb-f6fd-4c1d-bf2b-b77ef05f236c"),
        Name = "SPNHL",
        Url = "thespnhl.com",
        Tags = new Tags { "psn", "ea nhl" },
        Enabled = true,
    };

    public IReadOnlyCollection<Site> Sites
    {
        get => new Site[] { SPNHL };
    }
}