using Duthie.Data;
using Duthie.Types;
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
        context.Set<Site>().OrderBy(s => s.Name);

    public async Task<IEnumerable<Site>> FindAsync(string text = "", ICollection<string>? tags = null)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var query = CreateQuery(context).Where(s => s.Enabled);

            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(s => s.Id.ToString().ToLower().Equals(text.ToLower())
                    || s.Name.Replace(" ", "").ToLower().Equals(text.Replace(" ", "").ToLower()));

            var results = await query
                .OrderBy(s => s.Id.ToString().ToLower().Equals(text.ToLower()))
                .ThenBy(s => s.Name)
                .ToListAsync();

            return tags?.Count() > 0
                ? results.Where(s => tags.All(tag => s.Tags.Contains(tag)))
                : results;
        }
    }

    public async Task<Site?> GetAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).FirstOrDefaultAsync(s => s.Id == id);
        }
    }

    public Task<IEnumerable<Site>> GetAllAsync() =>
        _memoryCache.GetOrCreateAsync<IEnumerable<Site>>(new { type = GetType(), method = "GetAllAsync" }, async entry =>
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
}