using Duthie.Data.Comparers;
using Duthie.Data.Converters;
using Duthie.Types.Common;
using Duthie.Types.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data.Teams;

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

        model.Property(l => l.Tags)
            .HasConversion(new StringCollectionToJsonConverter<Tags>(), new StringCollectionValueComparer<Tags>());

        model.HasMany(t => t.LeagueTeams)
            .WithOne(t => t.Team);

        model.Ignore(t => t.Leagues);

        model.HasData(
            DefaultTeams.NHL
            .Concat(DefaultTeams.AHL)
            .Concat(DefaultTeams.CHL)
            .Concat(DefaultTeams.NBA)
        );
    }
}