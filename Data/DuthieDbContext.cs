#nullable disable

using System.Collections.Immutable;
using Duthie.Data.Comparers;
using Duthie.Data.Converters;
using Duthie.Data.Games;
using Duthie.Data.Guilds;
using Duthie.Data.Leagues;
using Duthie.Data.Sites;
using Duthie.Data.Teams;
using Duthie.Data.Watchers;
using Duthie.Types.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Duthie.Data;

public class DuthieDbContext : DbContext
{
    private IEnumerable<DataModel> DataModels { get; } = ImmutableList.Create<DataModel>(
        new GameModel(),
        new GuildAdminModel(),
        new GuildMessageModel(),
        new GuildModel(),
        new LeagueModel(),
        new LeagueStateModel(),
        new LeagueTeamModel(),
        new SiteModel(),
        new TeamModel(),
        new WatcherModel()
    );

    public DuthieDbContext(DbContextOptions<DuthieDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var model in DataModels)
            builder.Create(model);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            AddSqlite(builder);
    }

    private void AddSqlite(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // DateTimeOffset
            var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(DateTimeOffset)
                                                                        || p.PropertyType == typeof(DateTimeOffset?));

            foreach (var property in properties)
            {
                builder
                    .Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion(new DateTimeOffsetToStringConverter());
            }

            // ulong
            properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(ulong)
                                                                    || p.PropertyType == typeof(ulong?));

            foreach (var property in properties)
            {
                builder
                    .Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion<string>();
            }
        }
    }

    public Task RemoveRangeAsync(params object[] entities)
    {
        RemoveRange(entities);
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(IEnumerable<object> entities)
    {
        RemoveRange(entities);
        return Task.CompletedTask;
    }
}