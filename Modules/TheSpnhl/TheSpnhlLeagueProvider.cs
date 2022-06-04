using Duthie.Types.Common;
using Duthie.Types.Leagues;

namespace Duthie.Modules.TheSpnhl;

public class TheSpnhlLeagueProvider : ILeagueProvider
{
    public IReadOnlyCollection<League> Leagues
    {
        get => new League[]
        {
            new League
            {
                SiteId = TheSpnhlSiteProvider.SITE_ID,
                Id = new Guid("6991c990-a4fa-488b-884a-79b00e4e3577"),
                Name = "SPNHL",
                Info = new TheSpnhlLeagueInfo { LeagueType = "NHL" },
                Tags = new Tags { "psn", "ea nhl", "6v6" },
                Enabled = true
            },
        };
    }
}