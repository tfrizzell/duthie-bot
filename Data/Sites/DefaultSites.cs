using Duthie.Types;

namespace Duthie.Data;

internal static class DefaultSites
{
    public static readonly Site LeagueGaming = new Site
    {
        Id = new Guid("e3f25028-0a34-4430-a2a5-a1a7fab73b41"),
        Name = "leaguegaming.com",
        Tags = new string[] { "psn", "xbox", "ea nhl", "nba 2k", "fifa" },
        Enabled = true
    };

    public static readonly Site MyVirtualGaming = new Site
    {
        Id = new Guid("40a06d17-e48f-49f1-9184-7393f035322c"),
        Name = "myvirtualgaming.com",
        Tags = new string[] { "psn", "ea nhl" },
        Enabled = true
    };

    public static readonly Site SPNHL = new Site
    {
        Id = new Guid("c193a2eb-f6fd-4c1d-bf2b-b77ef05f236c"),
        Name = "thespnhl.com",
        Tags = new string[] { "psn", "ea nhl" },
        Enabled = true
    };
}