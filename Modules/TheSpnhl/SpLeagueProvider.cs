using Duthie.Types;

namespace Duthie.Modules.TheSpnhl;

public class SpLeagueProvider : ILeagueProvider
{
    public IReadOnlyCollection<League> Leagues
    {
        get => new League[]
        {
            new League
            {
                SiteId = SpSiteProvider.SITE_ID,
                Name = "SPNHL",
                Info = new SpLeagueInfo { LeagueId = "spnhl" },
                Tags = new Tags { "psn", "ea nhl", "6v6" },
                Enabled = true
            },
        };
    }
}