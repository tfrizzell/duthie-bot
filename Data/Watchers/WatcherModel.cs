using Duthie.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Duthie.Data;

public class WatcherModel : DataModel<Watcher>
{
    protected override void Create(EntityTypeBuilder<Watcher> model)
    {
        model.ToTable("Watchers");

        model.HasKey(w => w.Id);

        model.HasIndex(w => new { w.GuildId, w.LeagueId, w.TeamId, w.Type, w.ChannelId })
            .IsUnique();

        model.Property(w => w.Id)
            .ValueGeneratedOnAdd();
    }
}