using Duthie.Types.Guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data;

public class GuildModel : DataModel<Guild>
{
    protected override void Create(EntityTypeBuilder<Guild> model)
    {
        model.ToTable("Guilds");

        model.HasKey(s => s.Id);
    }
}