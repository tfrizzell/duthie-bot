using Duthie.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data;

public class GuildAdminModel : DataModel<GuildAdmin>
{
    protected override void Create(EntityTypeBuilder<GuildAdmin> model)
    {
        model.ToTable("GuildAdmins");

        model.HasKey(a => new { a.GuildId, a.MemberId });
    }
}