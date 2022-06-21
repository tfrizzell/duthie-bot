using Duthie.Types.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data.Leagues;

public class LeagueAffiliateModel : DataModel<LeagueAffiliate>
{
    protected override void Create(EntityTypeBuilder<LeagueAffiliate> model)
    {
        model.ToTable("LeagueAffiliates");

        model.HasKey(a => new { a.LeagueId, a.AffiliateId });

        model.HasOne(a => a.Affiliate)
            .WithMany()
            .HasForeignKey(a => a.AffiliateId);
    }
}