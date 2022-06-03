using Duthie.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data;

public class GuildMessageModel : DataModel<GuildMessage>
{
    protected override void Create(EntityTypeBuilder<GuildMessage> model)
    {
        model.ToTable("GuildMessages");

        model.HasKey(m => m.Id);

        model.Property(m => m.Id)
            .ValueGeneratedOnAdd();
    }
}