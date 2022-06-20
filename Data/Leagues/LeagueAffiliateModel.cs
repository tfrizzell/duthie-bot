using Duthie.Data.Comparers;
using Duthie.Data.Converters;
using Duthie.Types.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data.Leagues;

public class LeagueAffiliateModel : DataModel<LeagueAffiliate>
{
    protected override void Create(EntityTypeBuilder<LeagueAffiliate> model)
    {
        model.ToTable("LeagueAffiliates");

        model.HasKey(a => new { a.LeagueId, a.AffiliateLeagueId });

        model.HasOne(a => a.League)
            .WithMany()
            .HasForeignKey(a => a.LeagueId);

        model.HasOne(a => a.AffiliatedLeague)
            .WithMany()
            .HasForeignKey(a => a.AffiliateLeagueId);

        /*
                        constraints: table =>
                {
                    table.PrimaryKey("PK_LeagueAffiliates", x => new { x.LeagueId, x.AffiliateLeagueId });
                    table.ForeignKey(
                        name: "FK_LeagueAffiliates_Leagues_AffiliateLeagueId",
                        column: x => x.AffiliateLeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeagueAffiliates_Leagues_LeagueId",
                        column: x => x.LeagueId,
                        principalTable: "Leagues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
                */
    }
}