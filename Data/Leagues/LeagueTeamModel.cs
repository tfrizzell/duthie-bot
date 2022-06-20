using Duthie.Types.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data.Leagues;

public class LeagueTeamModel : DataModel<LeagueTeam>
{
    protected override void Create(EntityTypeBuilder<LeagueTeam> model)
    {
        model.ToTable("LeagueTeams");

        model.HasKey(t => new { t.LeagueId, t.TeamId });

        model.HasIndex(t => new { t.LeagueId, t.ExternalId })
            .IsUnique();

        model.Property(t => t.ExternalId)
            .HasColumnName("ExternalId");
    }
}