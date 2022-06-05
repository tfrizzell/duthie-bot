using Duthie.Types.Leagues;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data.Leagues;

public class LeagueStateModel : DataModel<LeagueState>
{
    protected override void Create(EntityTypeBuilder<LeagueState> model)
    {
        model.ToTable("LeagueState");

        model.HasKey(s => s.LeagueId);
    }
}