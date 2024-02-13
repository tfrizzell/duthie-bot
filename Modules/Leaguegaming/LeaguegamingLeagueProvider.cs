using Duthie.Types.Common;
using Duthie.Types.Leagues;

namespace Duthie.Modules.Leaguegaming;

public class LeaguegamingLeagueProvider : ILeagueProvider
{
    internal static readonly League LGHL = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("25e5037d-cf8c-4a36-852c-e3cec36a5dc5"),
        Name = "LGHL",
        ShortName = "LGHL",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l37.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 37 },
        Tags = new Tags { "crossplay", "xbox", "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    internal static readonly League LGAHL = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("981d1b21-fa47-4979-9684-13336ecb3f6c"),
        Name = "LGAHL",
        ShortName = "LGAHL",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l38.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 38 },
        Tags = new Tags { "crossplay", "xbox", "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    internal static readonly League LGECHL = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("5dc830f1-84b4-4e87-b6ce-7034fbd4b420"),
        Name = "LGECHL",
        ShortName = "LGECHL",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l84.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 84 },
        Tags = new Tags { "crossplay", "xbox", "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    internal static readonly League LGCHL = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("f5bbe441-7cc0-4de8-8960-b479113997b7"),
        Name = "LGCHL",
        ShortName = "LGCHL",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l39.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 39 },
        Tags = new Tags { "crossplay", "xbox", "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    internal static readonly League LGNCAA = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("73bff1b6-8177-44f8-878a-84b9f74eb728"),
        Name = "LGHL NCAA",
        ShortName = "LGNCAA",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l112.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 112 },
        Tags = new Tags { "crossplay", "xbox", "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    internal static readonly League LGHL_PSN = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("86c4e0fe-056b-450c-9a55-9ab32946ea31"),
        Name = "LGHL PSN",
        ShortName = "LGHL PSN",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l67.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 67 },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = false,
    };

    internal static readonly League LGAHL_PSN = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("c5884f38-cae4-461c-af99-beebcdc63e88"),
        Name = "LGAHL PSN",
        ShortName = "LGAHL PSN",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l68.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 68 },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = false,
    };

    internal static readonly League LGCHL_PSN = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("e6f88d50-c9e3-43f2-be3d-11c29fc4403b"),
        Name = "LGCHL PSN",
        ShortName = "LGCHL PSN",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l69.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 69 },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = false,
    };

    internal static readonly League LGNCAA_PSN = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("bff63f00-6cb6-4bea-a5ce-5abc3077fc96"),
        Name = "LGHL NCAA PSN",
        ShortName = "LGNCAA PSN",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l113.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 113 },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = false,
    };

    internal static readonly League ESHL_XBOX = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("aef1cea7-c626-42b4-9a45-0b9ea3deeb51"),
        Name = "ESHL",
        ShortName = "ESHL",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l90.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 90 },
        Tags = new Tags { "xbox", "ea nhl", "6v6", "esports" },
        Enabled = false,
    };

    internal static readonly League ESHL_PSN = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("0f9b50f8-3526-4bd3-9323-60b67f6a6abb"),
        Name = "ESHL PSN",
        ShortName = "ESHL PSN",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l91.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 91 },
        Tags = new Tags { "psn", "ea nhl", "6v6", "esports" },
        Enabled = false,
    };

    internal static readonly League LGWC_PSN = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("92718d97-8d2d-4ea3-a4b0-c4cefb75979d"),
        Name = "LG World Cup",
        ShortName = "LGWC",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l97.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 97 },
        Tags = new Tags { "psn", "ea nhl", "6v6", "tournament" },
        Enabled = false,
    };

    internal static readonly League LGFNP = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("76f28c43-fe50-4d66-910d-be37622ecb0e"),
        Name = "LGFNP",
        ShortName = "LGFNP",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l78.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 78 },
        Tags = new Tags { "crossplay", "xbox", "psn", "ea nhl", "6v6", "weekly", "pickup" },
        Enabled = true,
    };

    internal static readonly League LGFNP_PSN = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("f8ef5453-6b84-4ae9-9c3e-0553f0fd8971"),
        Name = "LGFNP PSN",
        ShortName = "LGFNP PSN",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l79.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 79 },
        Tags = new Tags { "psn", "ea nhl", "6v6", "weekly", "pickup" },
        Enabled = false,
    };

    internal static readonly League LGBA_XBOX = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("c0fcd9f5-d48a-465f-867b-905bafec917d"),
        Name = "LGBA XBOX",
        ShortName = "LGBA XBOX",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l50.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 50 },
        Tags = new Tags { "xbox", "nba 2k", "5v5" },
        Enabled = false,
    };

    internal static readonly League LGBA = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("3b5133d0-8801-4b86-9920-b7025cf88335"),
        Name = "LGBA",
        ShortName = "LGBA",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l70.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 70 },
        Tags = new Tags { "crossplay", "xbox", "psn", "nba 2k", "5v5" },
        Enabled = true,
    };

    internal static readonly League LGFA_XBOX = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("f9351c11-a36d-4069-804b-e0f317935576"),
        Name = "LGFA XBOX",
        ShortName = "LGFA XBOX",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l53.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 53 },
        Tags = new Tags { "xbox", "fifa", "11v11" },
        Enabled = false,
    };

    internal static readonly League LGFA = new League
    {
        SiteId = LeaguegamingSiteProvider.Leaguegaming.Id,
        Site = LeaguegamingSiteProvider.Leaguegaming,
        Id = new Guid("1112ece0-a84c-4dc1-9a75-278d4a0e4dd8"),
        Name = "LGFA",
        ShortName = "LGFA",
        LogoUrl = "https://www.leaguegaming.com/images/league/icon/l73.png",
        Info = new LeaguegamingLeagueInfo { LeagueId = 73 },
        Tags = new Tags { "crossplay", "xbox", "psn", "fifa", "11v11" },
        Enabled = true,
    };

    public LeaguegamingLeagueProvider()
    {
        LGHL.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate {
                LeagueId = LGHL.Id,
                League = LGHL,
                AffiliateId = LGAHL.Id,
                Affiliate = LGAHL,
            },
            new LeagueAffiliate {
                LeagueId = LGHL.Id,
                League = LGHL,
                AffiliateId = LGECHL.Id,
                Affiliate = LGECHL,
            },
            new LeagueAffiliate {
                LeagueId = LGHL.Id,
                League = LGHL,
                AffiliateId = LGCHL.Id,
                Affiliate = LGCHL,
            },
            new LeagueAffiliate {
                LeagueId = LGHL.Id,
                League = LGHL,
                AffiliateId = LGNCAA.Id,
                Affiliate = LGNCAA,
            },
        };

        LGAHL.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate {
                LeagueId = LGAHL.Id,
                League = LGAHL,
                AffiliateId = LGHL.Id,
                Affiliate = LGHL,
            },
            new LeagueAffiliate {
                LeagueId = LGAHL.Id,
                League = LGAHL,
                AffiliateId = LGECHL.Id,
                Affiliate = LGECHL,
            },
            new LeagueAffiliate {
                LeagueId = LGAHL.Id,
                League = LGAHL,
                AffiliateId = LGCHL.Id,
                Affiliate = LGCHL,
            },
            new LeagueAffiliate {
                LeagueId = LGAHL.Id,
                League = LGAHL,
                AffiliateId = LGNCAA.Id,
                Affiliate = LGNCAA,
            },
        };

        LGECHL.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate {
                LeagueId = LGECHL.Id,
                League = LGECHL,
                AffiliateId = LGHL.Id,
                Affiliate = LGHL,
            },
            new LeagueAffiliate {
                LeagueId = LGECHL.Id,
                League = LGECHL,
                AffiliateId = LGAHL.Id,
                Affiliate = LGAHL,
            },
            new LeagueAffiliate {
                LeagueId = LGECHL.Id,
                League = LGECHL,
                AffiliateId = LGCHL.Id,
                Affiliate = LGCHL,
            },
            new LeagueAffiliate {
                LeagueId = LGECHL.Id,
                League = LGECHL,
                AffiliateId = LGNCAA.Id,
                Affiliate = LGNCAA,
            },
        };

        LGCHL.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate {
                LeagueId = LGCHL.Id,
                League = LGCHL,
                AffiliateId = LGHL.Id,
                Affiliate = LGHL,
            },
            new LeagueAffiliate {
                LeagueId = LGCHL.Id,
                League = LGCHL,
                AffiliateId = LGAHL.Id,
                Affiliate = LGAHL,
            },
            new LeagueAffiliate {
                LeagueId = LGCHL.Id,
                League = LGCHL,
                AffiliateId = LGECHL.Id,
                Affiliate = LGECHL,
            },
            new LeagueAffiliate {
                LeagueId = LGCHL.Id,
                League = LGCHL,
                AffiliateId = LGNCAA.Id,
                Affiliate = LGNCAA,
            },
        };

        LGNCAA.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate {
                LeagueId = LGNCAA.Id,
                League = LGNCAA,
                AffiliateId = LGHL.Id,
                Affiliate = LGHL,
            },
            new LeagueAffiliate {
                LeagueId = LGNCAA.Id,
                League = LGNCAA,
                AffiliateId = LGAHL.Id,
                Affiliate = LGAHL,
            },
            new LeagueAffiliate {
                LeagueId = LGNCAA.Id,
                League = LGNCAA,
                AffiliateId = LGECHL.Id,
                Affiliate = LGECHL,
            },
            new LeagueAffiliate {
                LeagueId = LGNCAA.Id,
                League = LGNCAA,
                AffiliateId = LGCHL.Id,
                Affiliate = LGCHL,
            },
        };

        LGHL_PSN.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate {
                LeagueId = LGHL_PSN.Id,
                League = LGHL_PSN,
                AffiliateId = LGAHL_PSN.Id,
                Affiliate = LGAHL_PSN,
            },
            new LeagueAffiliate {
                LeagueId = LGHL_PSN.Id,
                League = LGHL_PSN,
                AffiliateId = LGCHL_PSN.Id,
                Affiliate = LGCHL_PSN,
            },
            new LeagueAffiliate {
                LeagueId = LGHL_PSN.Id,
                League = LGHL_PSN,
                AffiliateId = LGNCAA_PSN.Id,
                Affiliate = LGNCAA_PSN,
            },
        };

        LGAHL_PSN.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate {
                LeagueId = LGAHL_PSN.Id,
                League = LGAHL_PSN,
                AffiliateId = LGHL_PSN.Id,
                Affiliate = LGHL_PSN,
            },
            new LeagueAffiliate {
                LeagueId = LGAHL_PSN.Id,
                League = LGAHL_PSN,
                AffiliateId = LGCHL_PSN.Id,
                Affiliate = LGCHL_PSN,
            },
            new LeagueAffiliate {
                LeagueId = LGAHL_PSN.Id,
                League = LGAHL_PSN,
                AffiliateId = LGNCAA_PSN.Id,
                Affiliate = LGNCAA_PSN,
            },
        };

        LGCHL_PSN.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate {
                LeagueId = LGCHL_PSN.Id,
                League = LGCHL_PSN,
                AffiliateId = LGHL_PSN.Id,
                Affiliate = LGHL_PSN,
            },
            new LeagueAffiliate {
                LeagueId = LGCHL_PSN.Id,
                League = LGCHL_PSN,
                AffiliateId = LGAHL_PSN.Id,
                Affiliate = LGAHL_PSN,
            },
            new LeagueAffiliate {
                LeagueId = LGCHL_PSN.Id,
                League = LGCHL_PSN,
                AffiliateId = LGNCAA_PSN.Id,
                Affiliate = LGNCAA_PSN,
            },
        };

        LGNCAA_PSN.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate {
                LeagueId = LGNCAA_PSN.Id,
                League = LGNCAA_PSN,
                AffiliateId = LGHL_PSN.Id,
                Affiliate = LGHL_PSN,
            },
            new LeagueAffiliate {
                LeagueId = LGNCAA_PSN.Id,
                League = LGNCAA_PSN,
                AffiliateId = LGAHL_PSN.Id,
                Affiliate = LGAHL_PSN,
            },
            new LeagueAffiliate {
                LeagueId = LGNCAA_PSN.Id,
                League = LGNCAA_PSN,
                AffiliateId = LGCHL_PSN.Id,
                Affiliate = LGCHL_PSN,
            },
        };
    }

    public IReadOnlyCollection<League> Leagues
    {
        get => new League[]
        {
            LGHL,
            LGAHL,
            LGECHL,
            LGCHL,
            LGNCAA,
            LGHL_PSN,
            LGAHL_PSN,
            LGCHL_PSN,
            LGNCAA_PSN,
            ESHL_XBOX,
            ESHL_PSN,
            LGWC_PSN,
            LGFNP,
            LGFNP_PSN,
            LGBA_XBOX,
            LGBA,
            LGFA_XBOX,
            LGFA,
        };
    }
}