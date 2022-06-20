using Duthie.Types.Common;
using Duthie.Types.Leagues;

namespace Duthie.Modules.MyVirtualGaming;

public class MyVirtualGamingLeagueProvider : ILeagueProvider
{
    internal static readonly League VGNHL = new League
    {
        SiteId = MyVirtualGamingSiteProvider.VGHL.Id,
        Site = MyVirtualGamingSiteProvider.VGHL,
        Id = new Guid("5957b164-7bb5-4324-967a-16c3044260b2"),
        Name = "VGNHL National League",
        ShortName = "VGNHL",
        LogoUrl = "https://media.discordapp.net/attachments/436541687142285312/985399246763212820/vgnhl_white.png",
        Info = new MyVirtualGamingLeagueInfo
        {
            Features = MyVirtualGamingFeatures.All,
            LeagueId = "vgnhl",
        },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    internal static readonly League VGAHL = new League
    {
        SiteId = MyVirtualGamingSiteProvider.VGHL.Id,
        Site = MyVirtualGamingSiteProvider.VGHL,
        Id = new Guid("0fc1b6e9-9181-4545-9d32-5edbd67b276a"),
        Name = "VGAHL Affiliate League",
        ShortName = "VGAHL",
        LogoUrl = "https://media.discordapp.net/attachments/436541687142285312/985399247006470194/vgahl_white.png",
        Info = new MyVirtualGamingLeagueInfo
        {
            Features = MyVirtualGamingFeatures.RecentTransactions,
            LeagueId = "vgahl",
        },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    internal static readonly League VGPHL = new League
    {
        SiteId = MyVirtualGamingSiteProvider.VGHL.Id,
        Site = MyVirtualGamingSiteProvider.VGHL,
        Id = new Guid("ed4403ee-5ed3-46b2-8dce-d245c1e5b132"),
        Name = "VGPHL Prospect League",
        ShortName = "VGPHL",
        LogoUrl = "https://vghl.myvirtualgaming.com/images/Images/new_vgphl_logo_menu.svg",
        Info = new MyVirtualGamingLeagueInfo
        {
            Features = MyVirtualGamingFeatures.RecentTransactions,
            LeagueId = "vgphl",
        },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = false,
    };

    internal static readonly League VGHLWC = new League
    {
        SiteId = MyVirtualGamingSiteProvider.VGHL.Id,
        Site = MyVirtualGamingSiteProvider.VGHL,
        Id = new Guid("0ec6177f-7e39-437b-9cb9-1551db76bd4e"),
        Name = "VGHL World Championship",
        ShortName = "VGHLWC",
        LogoUrl = "https://vghl.myvirtualgaming.com/images/Images/new_vghlwc_logo_menu.svg",
        Info = new MyVirtualGamingLeagueInfo
        {
            Features = MyVirtualGamingFeatures.None,
            LeagueId = "vghlwc",
        },
        Tags = new Tags { "psn", "ea nhl", "6v6", "tournament" },
        Enabled = true,
    };

    internal static readonly League VGHLClub = new League
    {
        SiteId = MyVirtualGamingSiteProvider.VGHL.Id,
        Site = MyVirtualGamingSiteProvider.VGHL,
        Id = new Guid("8cba4eb0-8722-4415-aa82-b0027ae33702"),
        Name = "VGHL Club League",
        ShortName = "VGHL Club",
        LogoUrl = "https://vghl.myvirtualgaming.com/images/Images/new_vghlclub_logo_menu.svg",
        Info = new MyVirtualGamingLeagueInfo
        {
            Features = MyVirtualGamingFeatures.None,
            LeagueId = "vghlclub",
        },
        Tags = new Tags { "psn", "ea nhl", "6v6", "club teams" },
        Enabled = true,
    };

    internal static readonly League VGHL3s = new League
    {
        SiteId = MyVirtualGamingSiteProvider.VGHL.Id,
        Site = MyVirtualGamingSiteProvider.VGHL,
        Id = new Guid("9545ede8-6948-44e0-8ef8-61668c6ab9e1"),
        Name = "VGHL 3s League",
        ShortName = "VGHL 3s",
        LogoUrl = "https://media.discordapp.net/attachments/436541687142285312/985399501793681488/3sample1-6-1.png",
        Info = new MyVirtualGamingLeagueInfo
        {
            Features = MyVirtualGamingFeatures.RecentTransactions,
            LeagueId = "vghl3",
        },
        Tags = new Tags { "psn", "ea nhl", "3v3" },
        Enabled = true,
    };

    internal static readonly League VGIHL = new League
    {
        SiteId = MyVirtualGamingSiteProvider.VGHL.Id,
        Site = MyVirtualGamingSiteProvider.VGHL,
        Id = new Guid("cef6775d-f621-4164-a629-80ec54e016fa"),
        Name = "VGIHL International League",
        ShortName = "VGIHL",
        LogoUrl = "https://media.discordapp.net/attachments/436541687142285312/985399247698554900/vgihl_white.png",
        Info = new MyVirtualGamingLeagueInfo
        {
            Features = MyVirtualGamingFeatures.RecentTransactions,
            LeagueId = "vgihl",
        },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    public MyVirtualGamingLeagueProvider()
    {
        VGNHL.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate { AffiliatedLeague = VGAHL },
        };

        VGAHL.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate { AffiliatedLeague = VGNHL },
            new LeagueAffiliate { AffiliatedLeague = VGPHL },
        };

        VGPHL.Affiliates = new LeagueAffiliate[] {
            new LeagueAffiliate { AffiliatedLeague = VGAHL },
        };
    }

    public IReadOnlyCollection<League> Leagues
    {
        get => new League[]
        {
            VGNHL,
            VGAHL,
            VGPHL,
            VGHLWC,
            VGHLClub,
            VGHL3s,
            VGIHL,
        };
    }
}