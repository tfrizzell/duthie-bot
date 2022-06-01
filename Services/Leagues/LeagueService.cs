using Duthie.Data;
using Duthie.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Leagues;

public class LeagueService
{
    private readonly ILogger<LeagueService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;
    private readonly IMemoryCache _memoryCache;

    public LeagueService(
        ILogger<LeagueService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory,
        IMemoryCache memoryCache)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _memoryCache = memoryCache;
    }

    private IQueryable<League> CreateQuery(DuthieDbContext context) =>
        context.Set<League>()
            .Include(l => l.Site)
            .Include(l => l.LeagueTeams)
            .OrderBy(l => l.Name);

    public async Task<IEnumerable<League>> FindAsync(string text = "", IEnumerable<Guid>? sites = null, string[]? tags = null)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var query = CreateQuery(context).Where(l => l.Enabled);

            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(l => l.Id.ToString().ToLower().Equals(text.ToLower())
                    || l.Name.Replace(" ", "").ToLower().Equals(text.Replace(" ", "").ToLower()));

            if (sites?.Count() > 0)
                query = query.Where(l => sites.Contains(l.SiteId));

            var results = await query
                .OrderBy(l => l.Id.ToString().ToLower().Equals(text.ToLower()))
                .ThenBy(l => l.Name)
                .ToListAsync();

            return tags?.Count() > 0
                ? results.Where(l => tags.All(tag => l.Tags.Contains(tag)))
                : results;
        }
    }

    public async Task<League?> GetAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).FirstOrDefaultAsync(l => l.Id == id);
        }
    }

    public Task<IEnumerable<League>> GetAllAsync() =>
        _memoryCache.GetOrCreateAsync<IEnumerable<League>>(new { type = GetType(), method = "GetAllAsync" }, async entry =>
        {
            entry.SetOptions(new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15)
            });

            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                return await CreateQuery(context)
                    .Where(l => l.Enabled)
                    .ToListAsync();
            }
        });
}