using Duthie.Data.Comparers;
using Duthie.Data.Converters;
using Duthie.Types.Common;
using Duthie.Types.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data.Sites;

public class SiteModel : DataModel<Site>
{
    protected override void Create(EntityTypeBuilder<Site> model)
    {
        model.ToTable("Sites");

        model.HasKey(s => s.Id);

        model.HasIndex(s => s.Name)
            .IsUnique();

        model.Property(s => s.Id)
            .ValueGeneratedOnAdd();

        model.Property(l => l.Tags)
            .HasConversion(new StringCollectionToJsonConverter<Tags>(), new StringCollectionValueComparer<Tags>());
    }
}