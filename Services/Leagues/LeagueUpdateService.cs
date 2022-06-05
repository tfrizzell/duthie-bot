using System.Text.RegularExpressions;
using Duthie.Data;
using Duthie.Services.Api;
using Duthie.Types.Api;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Background;

// TODO: Rewrite this to use LeagueService and TeamService once the league saving issue
//       has been resolved.
public class LeagueUpdateService
{
    private readonly ILogger<LeagueUpdateService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;
    private readonly ApiService _apiService;

    public LeagueUpdateService(
        ILogger<LeagueUpdateService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory,
        ApiService apiService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _apiService = apiService;
    }

    public async Task UpdateAll()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var leagues = await GetLeagues(context);
            await UpdateInfo(context, leagues);
            await UpdateTeams(context, leagues);
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateInfo()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            await UpdateInfo(context);
            await context.SaveChangesAsync();
        }
    }

    private async Task UpdateInfo(DuthieDbContext context, IEnumerable<League>? leagues = null)
    {
        _logger.LogInformation("Updating information");
        leagues ??= await GetLeagues(context);

        await Task.WhenAll(leagues.Select(async league =>
        {
            var api = _apiService.Get<ILeagueInfoApi>(league);

            if (api == null)
                return;

            var data = await api.GetLeagueInfoAsync(league);

            if (data == null)
                return;

            league.Name = data.Name;
            league.Info = data.Info;
        }));
    }

    public async Task UpdateTeams()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            await UpdateTeams(context);
            await context.SaveChangesAsync();
        }
    }

    private async Task UpdateTeams(DuthieDbContext context, IEnumerable<League>? leagues = null)
    {
        _logger.LogInformation("Updating teams");
        leagues ??= await GetLeagues(context);
        var teams = await context.Set<Team>().ToDictionaryAsync(t => CreateKey(t.Name), StringComparer.OrdinalIgnoreCase);

        await Task.WhenAll(leagues.Select(async league =>
        {
            var api = _apiService.Get<ITeamApi>(league);

            if (api == null)
                return;

            var data = await api.GetTeamsAsync(league);

            if (data == null)
                return;

            foreach (var lt in data)
            {
                var key = CreateKey(lt.Team.Name);

                if (!teams.ContainsKey(key))
                {
                    teams.Add(key, lt.Team);
                    continue;
                }

                lt.TeamId = teams[key]?.Id ?? lt.TeamId;
                lt.Team = teams[key] ?? lt.Team;
            }

            league.LeagueTeams = data;
        }));
    }

    private async Task<IEnumerable<League>> GetLeagues(DuthieDbContext context) =>
        await context.Set<League>()
            .Include(l => l.Site)
            .Include(l => l.LeagueTeams)
            .Where(l => l.Enabled && l.Site.Enabled)
            .ToListAsync();

    private static string CreateKey(string name) =>
        Regex.Replace(name, @"[^0-9a-zA-Z]", "").ToUpper();
}