using System.Runtime.CompilerServices;
using Duthie.Types;

[assembly: InternalsVisibleTo("Duthie.Modules.LeagueGaming.Tests")]
namespace Duthie.Modules.LeagueGaming;

public class LeagueGamingSiteProvider : ISiteProvider
{
    internal static readonly Guid SITE_ID = new Guid("e3f25028-0a34-4430-a2a5-a1a7fab73b41");

    public IReadOnlyCollection<Site> Sites
    {
        get => new Site[]
        {
            new Site
            {
                Id = SITE_ID,
                Name = "leaguegaming.com",
                Tags = new Tags { "psn", "xbox", "ea nhl", "nba 2k", "fifa" },
                Enabled = true
            },
        };
    }
}