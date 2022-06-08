using Duthie.Types.Common;
using Duthie.Types.Leagues;

namespace Duthie.Modules.TheSpnhl;

public class TheSpnhlLeagueProvider : ILeagueProvider
{
    internal static readonly League SPNHL = new League
    {
        SiteId = TheSpnhlSiteProvider.SPNHL.Id,
        Site = TheSpnhlSiteProvider.SPNHL,
        Id = new Guid("6991c990-a4fa-488b-884a-79b00e4e3577"),
        Name = "SPNHL",
        ShortName = "SPNHL",
        LogoUrl = "https://thespnhl.com/wp-content/uploads/88F0D35F-4E35-492A-BD9F-E2EC610691F1.png",
        Info = new TheSpnhlLeagueInfo { LeagueType = "NHL" },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    public IReadOnlyCollection<League> Leagues
    {
        get => new League[]
        {
            SPNHL,
        };
    }
}