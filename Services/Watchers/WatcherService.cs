using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Watchers;

public class WatcherService
{
    private readonly ILogger<WatcherService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;
    private readonly IMemoryCache _memoryCache;

    public WatcherService(
        ILogger<WatcherService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory,
        IMemoryCache memoryCache)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _memoryCache = memoryCache;
    }

    private IQueryable<Watcher> CreateQuery(DuthieDbContext context) =>
        context.Set<Watcher>()
            .Include(w => w.League)
                .ThenInclude(l => l.Site)
            .Include(w => w.Team)
            .OrderBy(w => w.League.Name)
            .ThenBy(w => w.Team.Name)
            .ThenBy(w => w.Type);

    public async Task<int> DeleteAsync(params Guid[] ids)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var id in ids)
            {
                var watcher = await context.Set<Watcher>().FirstOrDefaultAsync(w => w.Id == id && w.ArchivedAt == null);

                if (watcher != null)
                    watcher.ArchivedAt = DateTimeOffset.UtcNow;
            }

            return await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await context.Set<Watcher>().AnyAsync(w => w.Id == id);
        }
    }

    public async Task<IEnumerable<Watcher>> FindAsync(ulong guildId, string text = "", IEnumerable<Guid>? sites = null, IEnumerable<Guid>? leagues = null, IEnumerable<Guid>? teams = null, IEnumerable<WatcherType>? types = null, IEnumerable<ulong?>? channels = null)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var query = CreateQuery(context).Where(w => w.ArchivedAt == null && w.GuildId == guildId);

            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(w => w.Id.ToString().ToLower().Equals(text.ToLower())
                    || w.League.Site.Name.Replace(" ", "").ToLower().Equals(text.Replace(" ", "").ToLower())
                    || w.League.Name.Replace(" ", "").ToLower().Equals(text.Replace(" ", "").ToLower())
                    || w.Team.Name.Replace(" ", "").ToLower().Equals(text.Replace(" ", "").ToLower()));

            if (sites?.Count() > 0)
                query = query.Where(w => sites.Contains(w.League.SiteId));

            if (leagues?.Count() > 0)
                query = query.Where(w => leagues.Contains(w.LeagueId));

            if (teams?.Count() > 0)
                query = query.Where(w => teams.Contains(w.TeamId));

            if (channels?.Count() > 0)
                query = query.Where(w => channels.Contains(w.ChannelId));

            return await query
                .OrderBy(w => w.Id.ToString().ToLower().Equals(text.ToLower()))
                .ThenBy(w => w.League.Name)
                .ThenBy(w => w.Team.Name)
                .ThenBy(w => w.Type)
                .ToListAsync();
        }
    }

    public async Task<Watcher?> GetAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).FirstOrDefaultAsync(w => w.Id == id);
        }
    }

    public async Task<IEnumerable<Watcher>> GetAllAsync(ulong guildId)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context)
                .Where(w => w.ArchivedAt == null && w.GuildId == guildId)
                .ToListAsync();
        }
    }

    public async Task PruneAsync()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var pruneFrom = DateTimeOffset.UtcNow.AddDays(-7);

            var toPrune = await context.Set<Watcher>()
                .Where(w => w.ArchivedAt != null && w.ArchivedAt <= pruneFrom.DateTime)
                .ToListAsync();

            if (toPrune.Count() > 0)
            {
                await context.Set<Watcher>().RemoveRangeAsync(toPrune);
                await context.SaveChangesAsync();
            }
        }
    }

    public async Task<int> SaveAsync(params Watcher[] watchers)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var watcher in watchers)
            {
                var existing = await context.Set<Watcher>().FirstOrDefaultAsync(w => w.GuildId == watcher.GuildId && w.LeagueId == watcher.LeagueId && w.TeamId == watcher.TeamId && w.Type == watcher.Type && w.ChannelId == watcher.ChannelId);

                if (existing != null && existing.ArchivedAt != null)
                    existing.ArchivedAt = null;
                else if (existing == null)
                    await context.Set<Watcher>().AddAsync(watcher);
            }

            return await context.SaveChangesAsync();
        }
    }
}