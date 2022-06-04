using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Teams;

public class TeamService
{
    private readonly ILogger<TeamService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;
    private readonly IMemoryCache _memoryCache;

    public TeamService(
        ILogger<TeamService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory,
        IMemoryCache memoryCache)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _memoryCache = memoryCache;
    }

    private IQueryable<Team> CreateQuery(DuthieDbContext context) =>
        context.Set<Team>()
            .Include(t => t.LeagueTeams)
                .ThenInclude(m => m.League)
                    .ThenInclude(l => l.Site)
            .OrderBy(t => t.Name);

    public async Task<int> DeleteAsync(IEnumerable<Guid> ids) =>
        await DeleteAsync(ids.ToArray());

    public async Task<int> DeleteAsync(params Guid[] ids)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var id in ids)
            {
                var team = await context.Set<Team>().FirstOrDefaultAsync(t => t.Id == id);

                if (team != null)
                    await context.Set<Team>().RemoveAsync(team);
            }

            return await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await context.Set<Team>().AnyAsync(t => t.Id == id);
        }
    }

    public async Task<IEnumerable<Team>> FindAsync(string text = "", IEnumerable<Guid>? sites = null, IEnumerable<Guid>? leagues = null, ICollection<string>? tags = null)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var query = CreateQuery(context);

            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(t => t.Id.ToString().ToLower() == text.ToLower()
                    || t.Name.Replace(" ", "").ToLower() == text.Replace(" ", "").ToLower()
                        || t.ShortName.Replace(" ", "").ToLower() == text.Replace(" ", "").ToLower());

            if (sites?.Count() > 0)
                query = query.Where(t => t.LeagueTeams.Any(m => sites.Contains(m.League.SiteId)));

            if (leagues?.Count() > 0)
                query = query.Where(t => t.LeagueTeams.Any(m => leagues.Contains(m.League.Id)));

            var teams = await query
                .OrderBy(t => t.Id.ToString().ToLower() == text.ToLower())
                    .ThenBy(t => t.Name)
                .ToListAsync();

            return tags?.Count() > 0
                ? teams.Where(t => tags.All(tag => t.Tags.Contains(tag)))
                : teams;
        }
    }

    public async Task<Team?> GetAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).FirstOrDefaultAsync(t => t.Id == id);
        }
    }

    public async Task<IEnumerable<Team>> GetAllAsync() =>
        await _memoryCache.GetOrCreateAsync<IEnumerable<Team>>(new { type = GetType(), method = "GetAllAsync" }, async entry =>
        {
            entry.SetOptions(new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15)
            });

            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                return await CreateQuery(context).ToListAsync();
            }
        });

    public async Task<int> SaveAsync(IEnumerable<Team> teams) =>
        await SaveAsync(teams.ToArray());

    public async Task<int> SaveAsync(params Team[] teams)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var team in teams)
            {
                if (!await context.Set<Team>().AnyAsync(t => t.Id == team.Id))
                    await context.Set<Team>().AddAsync(team);
                else
                    await context.Set<Team>().UpdateAsync(team);
            }

            return await context.SaveChangesAsync();
        }
    }
}