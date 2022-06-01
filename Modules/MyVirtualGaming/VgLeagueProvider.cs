using Duthie.Types;

namespace Duthie.Modules.MyVirtualGaming;

public class VgLeagueProvider : ILeagueProvider
{
    public IReadOnlyCollection<League> Leagues
    {
        get => new League[]
        {
            new League
            {
                SiteId = VgSiteProvider.SITE_ID,
                Id = new Guid("5957b164-7bb5-4324-967a-16c3044260b2"),
                Name = "VGNHL National League",
                Info = new VgLeagueInfo { LeagueId = "vgnhl" },
                Tags = new Tags { "psn", "ea nhl", "6v6" },
                Enabled = true
            },

            new League
            {
                SiteId = VgSiteProvider.SITE_ID,
                Id = new Guid("0fc1b6e9-9181-4545-9d32-5edbd67b276a"),
                Name = "VGAHL Affiliate League",
                Info = new VgLeagueInfo { LeagueId = "vgahl" },
                Tags = new Tags { "psn", "ea nhl", "6v6" },
                Enabled = true
            },

            new League
            {
                SiteId = VgSiteProvider.SITE_ID,
                Id = new Guid("ed4403ee-5ed3-46b2-8dce-d245c1e5b132"),
                Name = "VGPHL Prospect League",
                Info = new VgLeagueInfo { LeagueId = "vgphl" },
                Tags = new Tags { "psn", "ea nhl", "6v6" },
                Enabled = true
            },

            new League
            {
                SiteId = VgSiteProvider.SITE_ID,
                Id = new Guid("0ec6177f-7e39-437b-9cb9-1551db76bd4e"),
                Name = "VGHL World Championship",
                Info = new VgLeagueInfo { LeagueId = "vghlwc" },
                Tags = new Tags { "psn", "ea nhl", "6v6", "tournament" },
                Enabled = true
            },

            new League
            {
                SiteId = VgSiteProvider.SITE_ID,
                Id = new Guid("8cba4eb0-8722-4415-aa82-b0027ae33702"),
                Name = "VGHL Club League",
                Info = new VgLeagueInfo { LeagueId = "vghlclub" },
                Tags = new Tags { "psn", "ea nhl", "6v6" },
                Enabled = true
            },

            new League
            {
                SiteId = VgSiteProvider.SITE_ID,
                Id = new Guid("9545ede8-6948-44e0-8ef8-61668c6ab9e1"),
                Name = "VGHL 3s League",
                Info = new VgLeagueInfo { LeagueId = "vghl3" },
                Tags = new Tags { "psn", "ea nhl", "3v3" },
                Enabled = true
            },
        };
    }
}