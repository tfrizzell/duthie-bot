using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types.Leagues;
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
            .AsNoTracking()
            .Include(l => l.State)
            .Include(l => l.Site)
            .Include(l => l.LeagueTeams)
                .ThenInclude(t => t.Team)
            .OrderBy(l => l.Name);

    public async Task<int> DeleteAsync(IEnumerable<Guid> ids) =>
        await DeleteAsync(ids.ToArray());

    public async Task<int> DeleteAsync(params Guid[] ids)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var id in ids)
            {
                var league = await context.Set<League>().FirstOrDefaultAsync(l => l.Id == id && l.Enabled);

                if (league != null)
                    league.Enabled = false;
            }

            return await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await context.Set<League>().AnyAsync(l => l.Id == id && l.Enabled);
        }
    }

    public async Task<IEnumerable<League>> FindAsync(string text = "", IEnumerable<Guid>? sites = null, ICollection<string>? tags = null)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var query = CreateQuery(context).Where(l => l.Enabled && l.Site.Enabled);

            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(l => l.Id.ToString().ToLower() == text.ToLower()
                    || l.Name.Replace(" ", "").ToLower() == text.Replace(" ", "").ToLower()
                    || l.ShortName.Replace(" ", "").ToLower() == text.Replace(" ", "").ToLower());

            if (sites?.Count() > 0)
                query = query.Where(l => sites.Contains(l.SiteId));

            var leagues = await query
                .OrderBy(l => l.Id.ToString().ToLower() == text.ToLower())
                    .ThenBy(l => l.Name.Replace(" ", "").ToLower() == text.Replace(" ", "").ToLower())
                    .ThenBy(l => (l.Name.StartsWith("VG") && l.Name.ToLower().StartsWith(text.ToLower())))
                    .ThenBy(l => l.Name)
                .ToListAsync();

            return tags?.Count() > 0
                ? leagues.Where(l => tags.All(tag => l.Tags.Contains(tag)))
                : leagues;
        }
    }

    public async Task<League?> GetAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).FirstOrDefaultAsync(l => l.Id == id);
        }
    }

    public async Task<IEnumerable<League>> GetAllAsync() =>
        await _memoryCache.GetOrCreateAsync<IEnumerable<League>>(new { type = GetType(), method = "GetAllAsync" }, async entry =>
        {
            entry.SetOptions(new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15)
            });

            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                return await CreateQuery(context)
                    .Where(l => l.Enabled && l.Site.Enabled)
                    .ToListAsync();
            }
        });

    public async Task<int> SaveAsync(IEnumerable<League> leagues) =>
        await SaveAsync(leagues.ToArray());

    public async Task<int> SaveAsync(params League[] leagues)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var league in leagues)
            {
                var existing = await context.Set<League>()
                    .Include(l => l.State)
                    .Include(l => l.LeagueTeams)
                    .FirstOrDefaultAsync(l => l.Id == league.Id);

                if (existing != null)
                {
                    context.Entry(existing).CurrentValues.SetValues(league);
                    context.Entry(existing.State).CurrentValues.SetValues(league.State);
                    existing.LeagueTeams = await UpdateTeamsAsync(context, league);
                }
                else
                    await context.Set<League>().AddAsync(league);
            }

            return await context.SaveChangesAsync();
        }
    }

    public async Task<int> SaveStateAsync(Guid id, LeagueStateType stateType, string? value) =>
        await SaveStateAsync(id, new Dictionary<LeagueStateType, string?> { { stateType, value } });

    public async Task<int> SaveStateAsync(Guid id, IDictionary<LeagueStateType, string?> states)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var league = await context.Set<League>()
                .Include(l => l.State)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (league == null)
                return 0;

            foreach (var state in states)
            {
                switch (state.Key)
                {
                    case LeagueStateType.Bid:
                        league.State.LastBid = state.Value;
                        break;

                    case LeagueStateType.Contract:
                        league.State.LastContract = state.Value;
                        break;

                    case LeagueStateType.DraftPick:
                        league.State.LastDraftPick = state.Value;
                        break;

                    case LeagueStateType.Trade:
                        league.State.LastTrade = state.Value;
                        break;
                }
            }

            return await context.SaveChangesAsync();
        }
    }

    private async Task<IEnumerable<LeagueTeam>> UpdateTeamsAsync(DuthieDbContext context, League league)
    {
        var oldTeams = await context.Set<LeagueTeam>()
            .Where(lt => lt.LeagueId == league.Id)
            .ToListAsync();

        foreach (var team in oldTeams)
            context.Entry(team).State = EntityState.Deleted;

        foreach (var team in league.LeagueTeams)
            context.Entry(team).State = EntityState.Added;

        return league.LeagueTeams;
    }
}