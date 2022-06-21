using Duthie.Data.Comparers;
using Duthie.Data.Converters;
using Duthie.Types.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data.Leagues;

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

        model.Property(l => l.Tags)
            .HasConversion(new TagsToJsonConverter(), new TagsValueComparer());
    }
}