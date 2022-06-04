using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Games;

public class GameService
{
    private readonly ILogger<GameService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;

    public GameService(
        ILogger<GameService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;
    }

    private IQueryable<Game> CreateQuery(DuthieDbContext context) =>
        context.Set<Game>()
            .Include(g => g.League)
                .ThenInclude(l => l.Site)
            .Include(g => g.VisitorTeam)
            .Include(g => g.HomeTeam)
            .OrderBy(g => g.League.Name)
                .ThenBy(g => g.GameId);

    public async Task<int> DeleteAsync(IEnumerable<Guid> ids) =>
        await DeleteAsync(ids.ToArray());

    public async Task<int> DeleteAsync(params Guid[] ids)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var id in ids)
            {
                var game = await context.Set<Game>().FirstOrDefaultAsync(g => g.Id == id);

                if (game != null)
                    await context.Set<Game>().RemoveAsync(game);
            }

            return await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await context.Set<Game>()
                .Include(g => g.League)
                    .ThenInclude(l => l.Site)
                .AnyAsync(g => g.Id == id && g.League.Enabled && g.League.Site.Enabled);
        }
    }

    public async Task<Game?> GetAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).FirstOrDefaultAsync(l => l.Id == id);
        }
    }

    public async Task<IEnumerable<Game>> GetAllAsync()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context)
                .Where(g => g.League.Enabled && g.League.Site.Enabled)
                .ToListAsync();
        }
    }

    public async Task<IEnumerable<Game>> GetAllAsync(Guid leagueId)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context)
                .Where(g => g.LeagueId == leagueId && g.League.Enabled && g.League.Site.Enabled)
                .ToListAsync();
        }
    }

    public async Task<Game?> GetByGameIdAsync(Guid leagueId, ulong gameId)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).FirstOrDefaultAsync(g => g.LeagueId == leagueId && g.GameId == gameId);
        }
    }

    public async Task<int> SaveAsync(IEnumerable<Game> games) =>
        await SaveAsync(games.ToArray());

    public async Task<int> SaveAsync(params Game[] games)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var game in games)
            {
                if (!await context.Set<Game>().AnyAsync(g => g.Id == game.Id))
                    await context.Set<Game>().AddAsync(game);
                else
                    await context.Set<Game>().UpdateAsync(game);
            }

            return await context.SaveChangesAsync();
        }
    }
}