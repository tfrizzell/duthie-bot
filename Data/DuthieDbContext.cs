#nullable disable

using System.Collections.Immutable;
using Duthie.Data.Comparers;
using Duthie.Data.Converters;
using Duthie.Types.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Duthie.Data;

public class DuthieDbContext : DbContext
{
    private IEnumerable<DataModel> DataModels { get; } = ImmutableList.Create<DataModel>(
        new GuildModel(),
        new GuildAdminModel(),
        new SiteModel(),
        new LeagueModel(),
        new TeamModel(),
        new LeagueTeamModel(),
        new WatcherModel(),
        new GuildMessageModel(),
        new GameModel()
    );

    public DuthieDbContext(DbContextOptions<DuthieDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        foreach (var model in DataModels)
            builder.Create(model);

        AddConverters(builder);

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            AddSqlite(builder);
    }

    private void AddConverters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            // ulong
            var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(ulong)
                                                                        || p.PropertyType == typeof(ulong?));

            foreach (var property in properties)
            {
                builder
                    .Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion(new UlongToStringConverter(), new UlongValueComparer());
            }

            // Tags
            properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(Tags));

            foreach (var property in properties)
            {
                builder
                    .Entity(entityType.Name)
                    .Property(property.Name)
                    .HasConversion(new TagsToStringConverter(), new TagsValueComparer());
            }
        }
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