using Duthie.Types.Teams;

namespace Duthie.Types.Leagues;

public class LeagueAffiliate
{
    public Guid LeagueId { get; set; }
    public Guid AffiliateId { get; set; }

#nullable disable
    public virtual League League { get; set; }
    public virtual League Affiliate { get; set; }
#nullable enable
}