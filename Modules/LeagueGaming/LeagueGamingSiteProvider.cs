using Duthie.Types.Common;
using Duthie.Types.Sites;

namespace Duthie.Modules.LeagueGaming;

public class LeagueGamingSiteProvider : ISiteProvider
{
    internal static readonly Site Leaguegaming = new Site
    {
        Id = new Guid("e3f25028-0a34-4430-a2a5-a1a7fab73b41"),
        Name = "Leaguegaming",
        Url = "leaguegaming.com",
        Tags = new Tags { "psn", "xbox", "ea nhl", "nba 2k", "fifa" },
        Enabled = true,
    };

    public IReadOnlyCollection<Site> Sites
    {
        get => new Site[] { Leaguegaming };
    }
}