using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types.Guilds;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Guilds;

public class GuildService
{
    private readonly ILogger<GuildService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;

    public GuildService(
        ILogger<GuildService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;
    }

    private IQueryable<Guild> CreateQuery(DuthieDbContext context) =>
        context.Set<Guild>()
            .AsNoTracking()
            .OrderBy(g => g.Name);

    public async Task<int> DeleteAsync(IEnumerable<ulong> ids) =>
        await DeleteAsync(ids.ToArray());

    public async Task<int> DeleteAsync(params ulong[] ids)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var id in ids)
            {
                var guild = await context.Set<Guild>().FirstOrDefaultAsync(g => g.Id == id);

                if (guild != null)
                    guild.LeftAt = DateTimeOffset.UtcNow;
            }

            return await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(ulong id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await context.Set<Guild>().AnyAsync(g => g.Id == id && g.LeftAt == null);
        }
    }

    public async Task<Guild?> GetAsync(ulong id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).FirstOrDefaultAsync(g => g.Id == id);
        }
    }

    public async Task<IEnumerable<Guild>> GetAllAsync()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context)
                .Where(g => g.LeftAt == null)
                .ToListAsync();
        }
    }

    public async Task<int> PruneAsync()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var pruneFrom = DateTimeOffset.UtcNow.AddDays(-7);

            var toPrune = await context.Set<Guild>()
                .Where(g => g.LeftAt != null && g.LeftAt <= pruneFrom.DateTime)
                .ToListAsync();

            if (toPrune.Count() > 0)
            {
                await context.Set<Guild>().RemoveRangeAsync(toPrune);
                return await context.SaveChangesAsync();
            }
        }

        return 0;
    }

    public async Task<int> SaveAsync(IEnumerable<Guild> guilds) =>
        await SaveAsync(guilds.ToArray());

    public async Task<int> SaveAsync(params Guild[] guilds)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var guild in guilds)
            {
                var existing = await context.Set<Guild>().FirstOrDefaultAsync(g => g.Id == guild.Id);

                if (existing != null)
                    context.Entry(existing).CurrentValues.SetValues(guild);
                else
                {
                    guild.JoinedAt = DateTimeOffset.UtcNow;
                    guild.LeftAt = null;
                    await context.Set<Guild>().AddAsync(guild);
                }
            }

            return await context.SaveChangesAsync();
        }
    }
}