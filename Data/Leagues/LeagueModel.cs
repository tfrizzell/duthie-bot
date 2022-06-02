using Duthie.Data.Comparers;
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

        model.HasIndex(l => l.Name)
            .IsUnique();

        model.Property(l => l.Id)
            .ValueGeneratedOnAdd();

        model.Property(l => l.Info)
            .HasConversion(new LeagueInfoToStringConverter(), new LeagueInfoValueComparer());

        model.HasMany(l => l.LeagueTeams)
            .WithOne(t => t.League);

        model.Ignore(l => l.Teams);
    }
}