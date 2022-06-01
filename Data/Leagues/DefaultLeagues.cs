using Duthie.Types;

namespace Duthie.Data;

internal static class DefaultLeagues
{
    public static readonly League LGHL = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("25e5037d-cf8c-4a36-852c-e3cec36a5dc5"),
        Name = "LGHL",
        Tags = new string[] { "xbox", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League LGAHL = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("981d1b21-fa47-4979-9684-13336ecb3f6c"),
        Name = "LGAHL",
        Tags = new string[] { "xbox", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League LGCHL = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("f5bbe441-7cc0-4de8-8960-b479113997b7"),
        Name = "LGCHL",
        Tags = new string[] { "xbox", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League LGHL_PSN = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("86c4e0fe-056b-450c-9a55-9ab32946ea31"),
        Name = "LGHL PSN",
        Tags = new string[] { "psn", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League LGAHL_PSN = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("c5884f38-cae4-461c-af99-beebcdc63e88"),
        Name = "LGAHL PSN",
        Tags = new string[] { "psn", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League LGCHL_PSN = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("e6f88d50-c9e3-43f2-be3d-11c29fc4403b"),
        Name = "LGCHL PSN",
        Tags = new string[] { "psn", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League ESHL = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("aef1cea7-c626-42b4-9a45-0b9ea3deeb51"),
        Name = "ESHL",
        Tags = new string[] { "xbox", "ea nhl", "6v6", "esports" },
        Enabled = true
    };

    public static readonly League ESHL_PSN = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("0f9b50f8-3526-4bd3-9323-60b67f6a6abb"),
        Name = "ESHL PSN",
        Tags = new string[] { "psn", "ea nhl", "6v6", "esports" },
        Enabled = true
    };

    public static readonly League LGWORLDCUP = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("92718d97-8d2d-4ea3-a4b0-c4cefb75979d"),
        Name = "LG World Cup",
        Tags = new string[] { "psn", "ea nhl", "6v6", "tournament" },
        Enabled = true
    };

    public static readonly League VGNHL = new League
    {
        SiteId = DefaultSites.MyVirtualGaming.Id,
        Id = new Guid("5957b164-7bb5-4324-967a-16c3044260b2"),
        Name = "VGNHL National League",
        Tags = new string[] { "psn", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League VGAHL = new League
    {
        SiteId = DefaultSites.MyVirtualGaming.Id,
        Id = new Guid("0fc1b6e9-9181-4545-9d32-5edbd67b276a"),
        Name = "VGAHL Affiliate League",
        Tags = new string[] { "psn", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League VGPHL = new League
    {
        SiteId = DefaultSites.MyVirtualGaming.Id,
        Id = new Guid("ed4403ee-5ed3-46b2-8dce-d245c1e5b132"),
        Name = "VGPHL Prospect League",
        Tags = new string[] { "psn", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League VGWC = new League
    {
        SiteId = DefaultSites.MyVirtualGaming.Id,
        Id = new Guid("0ec6177f-7e39-437b-9cb9-1551db76bd4e"),
        Name = "VGHL World Championship",
        Tags = new string[] { "psn", "ea nhl", "6v6", "tournament" },
        Enabled = true
    };

    public static readonly League VGCLUB = new League
    {
        SiteId = DefaultSites.MyVirtualGaming.Id,
        Id = new Guid("8cba4eb0-8722-4415-aa82-b0027ae33702"),
        Name = "VGHL Club League",
        Tags = new string[] { "psn", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League SPNHL = new League
    {
        SiteId = DefaultSites.SPNHL.Id,
        Id = new Guid("d103fab3-808e-4451-a3c9-450534d5a4cb"),
        Name = "SPNHL",
        Tags = new string[] { "psn", "ea nhl", "6v6" },
        Enabled = true
    };

    public static readonly League LGFNP = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("76f28c43-fe50-4d66-910d-be37622ecb0e"),
        Name = "Friday Night Puck",
        Tags = new string[] { "xbox", "ea nhl", "6v6", "weekly", "pickup" },
        Enabled = true
    };

    public static readonly League LGFNP_PSN = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("f8ef5453-6b84-4ae9-9c3e-0553f0fd8971"),
        Name = "Friday Night Puck PSN",
        Tags = new string[] { "psn", "ea nhl", "6v6", "weekly", "pickup" },
        Enabled = true
    };

    public static readonly League LGBA = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("c0fcd9f5-d48a-465f-867b-905bafec917d"),
        Name = "LGBA",
        Tags = new string[] { "xbox", "nba 2k", "5v5" },
        Enabled = true
    };

    public static readonly League LGBA_PSN = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("3b5133d0-8801-4b86-9920-b7025cf88335"),
        Name = "LGBA PSN",
        Tags = new string[] { "psn", "nba 2k", "5v5" },
        Enabled = true
    };

    public static readonly League LGFA = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("f9351c11-a36d-4069-804b-e0f317935576"),
        Name = "LGFA",
        Tags = new string[] { "xbox", "fifa", "11v11" },
        Enabled = true
    };

    public static readonly League LGFA_PSN = new League
    {
        SiteId = DefaultSites.LeagueGaming.Id,
        Id = new Guid("1112ece0-a84c-4dc1-9a75-278d4a0e4dd8"),
        Name = "LGFA PSN",
        Tags = new string[] { "psn", "fifa", "11v11" },
        Enabled = true
    };

    public static readonly League VG_THREES = new League
    {
        SiteId = DefaultSites.MyVirtualGaming.Id,
        Id = new Guid("9545ede8-6948-44e0-8ef8-61668c6ab9e1"),
        Name = "VGHL 3s League",
        Tags = new string[] { "psn", "ea nhl", "3v3" },
        Enabled = true
    };
}