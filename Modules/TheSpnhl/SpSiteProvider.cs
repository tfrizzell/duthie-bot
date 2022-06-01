using Duthie.Types;

namespace Duthie.Modules.TheSpnhl;

public class SpSiteProvider : ISiteProvider
{
    internal static readonly Guid SITE_ID = new Guid("c193a2eb-f6fd-4c1d-bf2b-b77ef05f236c");

    public IReadOnlyCollection<Site> Sites
    {
        get => new Site[]
        {
            new Site
            {
                Id = SITE_ID,
                Name = "thespnhl.com",
                Tags = new Tags { "psn", "ea nhl" },
                Enabled = true
            },
        };
    }
}