using System.ComponentModel.DataAnnotations.Schema;

namespace Duthie.Types.Leagues;

public class LeagueAffiliate
{
    public Guid LeagueId { get; set; }
    public Guid AffiliateLeagueId { get; set; }

#nullable disable
    public virtual League League { get; set; }
    public virtual League AffiliatedLeague { get; set; }
#nullable enable
}