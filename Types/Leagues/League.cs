using Duthie.Types.Common;
using Duthie.Types.Sites;
using Duthie.Types.Teams;

namespace Duthie.Types.Leagues;

public class League
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public string Name { get; set; } = "";
    public string ShortName { get; set; } = "";
    public string? LogoUrl { get; set; }
    public object? Info { get; set; }
    public LeagueState State { get; set; } = new LeagueState();
    public Tags Tags { get; set; } = new Tags();
    public bool Enabled { get; set; } = true;

#nullable disable
    public virtual Site Site { get; set; }
    public virtual IEnumerable<LeagueTeam> Teams { get; set; }
    public virtual IEnumerable<LeagueAffiliate> Affiliates { get; set; }
#nullable enable
}