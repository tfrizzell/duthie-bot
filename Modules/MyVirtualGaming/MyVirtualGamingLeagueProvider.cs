using Duthie.Types.Common;
using Duthie.Types.Leagues;

namespace Duthie.Modules.MyVirtualGaming;

public class MyVirtualGamingLeagueProvider : ILeagueProvider
{
    internal static readonly League VGNHL = new League
    {
        SiteId = MyVirtualGamingSiteProvider.MyVirtualGaming.Id,
        Id = new Guid("5957b164-7bb5-4324-967a-16c3044260b2"),
        Name = "VGNHL National League",
        Info = new MyVirtualGamingLeagueInfo
        {
            LeagueId = "vgnhl",
            Features = MyVirtualGamingFeatures.All,
        },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    internal static readonly League VGAHL = new League
    {
        SiteId = MyVirtualGamingSiteProvider.MyVirtualGaming.Id,
        Id = new Guid("0fc1b6e9-9181-4545-9d32-5edbd67b276a"),
        Name = "VGAHL Affiliate League",
        Info = new MyVirtualGamingLeagueInfo
        {
            LeagueId = "vgahl",
            Features = MyVirtualGamingFeatures.All,
        },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    internal static readonly League VGPHL = new League
    {
        SiteId = MyVirtualGamingSiteProvider.MyVirtualGaming.Id,
        Id = new Guid("ed4403ee-5ed3-46b2-8dce-d245c1e5b132"),
        Name = "VGPHL Prospect League",
        Info = new MyVirtualGamingLeagueInfo
        {
            LeagueId = "vgphl",
            Features = MyVirtualGamingFeatures.All,
        },
        Tags = new Tags { "psn", "ea nhl", "6v6" },
        Enabled = true,
    };

    internal static readonly League VGHLWC = new League
    {
        SiteId = MyVirtualGamingSiteProvider.MyVirtualGaming.Id,
        Id = new Guid("0ec6177f-7e39-437b-9cb9-1551db76bd4e"),
        Name = "VGHL World Championship",
        Info = new MyVirtualGamingLeagueInfo
        {
            LeagueId = "vghlwc",
            Features = MyVirtualGamingFeatures.None,
        },
        Tags = new Tags { "psn", "ea nhl", "6v6", "tournament" },
        Enabled = true,
    };

    internal static readonly League VGHLClub = new League
    {
        SiteId = MyVirtualGamingSiteProvider.MyVirtualGaming.Id,
        Id = new Guid("8cba4eb0-8722-4415-aa82-b0027ae33702"),
        Name = "VGHL Club League",
        Info = new MyVirtualGamingLeagueInfo
        {
            LeagueId = "vghlclub",
            Features = MyVirtualGamingFeatures.None,
        },
        Tags = new Tags { "psn", "ea nhl", "6v6", "club teams" },
        Enabled = true,
    };

    internal static readonly League VGHL3s = new League
    {
        SiteId = MyVirtualGamingSiteProvider.MyVirtualGaming.Id,
        Id = new Guid("9545ede8-6948-44e0-8ef8-61668c6ab9e1"),
        Name = "VGHL 3s League",
        Info = new MyVirtualGamingLeagueInfo
        {
            LeagueId = "vghl3",
            Features = MyVirtualGamingFeatures.All,
        },
        Tags = new Tags { "psn", "ea nhl", "3v3" },
        Enabled = true,
    };

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
        };
    }
}