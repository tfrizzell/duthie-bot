using Duthie.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data;

public class TeamModel : DataModel<Team>
{
    protected override void Create(EntityTypeBuilder<Team> model)
    {
        model.ToTable("Teams");

        model.HasKey(t => t.Id);

        model.HasIndex(t => t.Name)
            .IsUnique();

        model.Property(t => t.Id)
            .ValueGeneratedOnAdd();

        model.Ignore(t => t.Leagues);

        model.HasData(
            DefaultTeams.NHL
            .Concat(DefaultTeams.AHL)
            .Concat(DefaultTeams.CHL)
            .Concat(DefaultTeams.NBA)
        );
    }
}