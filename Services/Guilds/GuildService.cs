using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types;
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
        context.Set<Guild>().OrderBy(g => g.Name);

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

    public async Task<IEnumerable<Guild>> FindAsync(string text = "")
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var query = CreateQuery(context).Where(g => g.LeftAt == null);

            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(g => g.Id.ToString().ToLower().Equals(text.ToLower())
                    || g.Name.Replace(" ", "").ToLower().Equals(text.Replace(" ", "").ToLower()));

            return await query
                .OrderBy(g => g.Id.ToString().ToLower().Equals(text.ToLower()))
                .ThenBy(g => g.Name)
                .ToListAsync();
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

    public async Task<bool> RenameAsync(ulong id, string name)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var guild = await context.Set<Guild>().FirstOrDefaultAsync(g => g.Id == id && g.LeftAt == null);

            if (guild == null)
                return false;

            guild.Name = name;
            return (await context.SaveChangesAsync()) > 0;
        }
    }

    public async Task<int> SaveAsync(params Guild[] guilds)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var guild in guilds)
            {
                if (!await context.Set<Guild>().AnyAsync(g => g.Id == guild.Id))
                {
                    guild.JoinedAt = DateTimeOffset.UtcNow;
                    guild.LeftAt = null;
                    await context.Set<Guild>().AddAsync(guild);
                }
                else
                    await context.Set<Guild>().UpdateAsync(guild);
            }

            return await context.SaveChangesAsync();
        }
    }
}