using Duthie.Data.Converters;
using Duthie.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data;

public class LeagueModel : DataModel<League>
{
    protected override void Create(EntityTypeBuilder<League> model)
    {
        model.ToTable("Leagues");

        model.HasKey(l => l.Id);

        model.HasIndex(l => new { l.SiteId, l.Name })
            .IsUnique();

        model.Property(l => l.Id)
            .ValueGeneratedOnAdd();

        model.Ignore(l => l.Teams);

        model.HasData(
            DefaultLeagues.LGHL,
            DefaultLeagues.LGAHL,
            DefaultLeagues.LGCHL,
            DefaultLeagues.LGHL_PSN,
            DefaultLeagues.LGAHL_PSN,
            DefaultLeagues.LGCHL_PSN,
            DefaultLeagues.ESHL,
            DefaultLeagues.ESHL_PSN,
            DefaultLeagues.LGWORLDCUP,
            DefaultLeagues.VGNHL,
            DefaultLeagues.VGAHL,
            DefaultLeagues.VGPHL,
            DefaultLeagues.VGWC,
            DefaultLeagues.VGCLUB,
            DefaultLeagues.SPNHL,
            DefaultLeagues.LGFNP,
            DefaultLeagues.LGFNP_PSN,
            DefaultLeagues.LGBA,
            DefaultLeagues.LGBA_PSN,
            DefaultLeagues.LGFA,
            DefaultLeagues.LGFA_PSN,
            DefaultLeagues.VG_THREES
        );
    }
}