using Duthie.Types.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data;

public class GameModel : DataModel<Game>
{
    protected override void Create(EntityTypeBuilder<Game> model)
    {
        model.ToTable("Games");

        model.HasKey(s => s.Id);

        model.Property(m => m.Id)
            .ValueGeneratedOnAdd();
    }
}