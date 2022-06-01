using Duthie.Modules.LeagueGaming;
using Duthie.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data;

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
    }
}