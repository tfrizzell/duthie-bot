using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types.Sites;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Sites;

public class SiteService
{
    private readonly ILogger<SiteService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;
    private readonly IMemoryCache _memoryCache;

    public SiteService(
        ILogger<SiteService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory,
        IMemoryCache memoryCache)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _memoryCache = memoryCache;
    }

    private IQueryable<Site> CreateQuery(DuthieDbContext context) =>
        context.Set<Site>()
            .AsNoTracking()
            .OrderBy(s => s.Name);

    public async Task<int> DeleteAsync(IEnumerable<Guid> ids) =>
        await DeleteAsync(ids.ToArray());

    public async Task<int> DeleteAsync(params Guid[] ids)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var id in ids)
            {
                var site = await context.Set<Site>().FirstOrDefaultAsync(s => s.Id == id && s.Enabled);

                if (site != null)
                    site.Enabled = false;
            }

            return await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await context.Set<Site>().AnyAsync(s => s.Id == id && s.Enabled);
        }
    }

    public async Task<IEnumerable<Site>> FindAsync(string text = "", ICollection<string>? tags = null)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var query = CreateQuery(context).Where(s => s.Enabled);

            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(s => s.Id.ToString().ToLower() == text.ToLower()
                    || s.Name.Replace(" ", "").ToLower() == text.Replace(" ", "").ToLower()
                    || s.Url.Replace(" ", "").ToLower() == text.Replace(" ", "").ToLower());

            var sites = await query
                .OrderBy(s => s.Id.ToString().ToLower() == text.ToLower())
                    .ThenBy(s => s.Name)
                .ToListAsync();

            return tags?.Count() > 0
                ? sites.Where(s => tags.All(tag => s.Tags.Contains(tag)))
                : sites;
        }
    }

    public async Task<Site?> GetAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).FirstOrDefaultAsync(s => s.Id == id);
        }
    }

    public async Task<IEnumerable<Site>> GetAllAsync() =>
        await _memoryCache.GetOrCreateAsync<IEnumerable<Site>>(new { type = GetType(), method = "GetAllAsync" }, async entry =>
        {
            entry.SetOptions(new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15)
            });

            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                return await CreateQuery(context)
                    .Where(s => s.Enabled)
                    .ToListAsync();
            }
        });

    public async Task<int> SaveAsync(IEnumerable<Site> sites) =>
        await SaveAsync(sites.ToArray());

    public async Task<int> SaveAsync(params Site[] sites)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var site in sites)
            {
                var existing = await context.Set<Site>().FirstOrDefaultAsync(s => s.Id == site.Id);

                if (existing != null)
                    context.Entry(existing).CurrentValues.SetValues(site);
                else
                    await context.Set<Site>().AddAsync(site);
            }

            return await context.SaveChangesAsync();
        }
    }
}