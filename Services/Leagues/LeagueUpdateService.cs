using System.Text.RegularExpressions;
using Duthie.Bot.Services;
using Duthie.Data;
using Duthie.Services.Leagues;
using Duthie.Types;
using Duthie.Types.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Background;

public class LeagueUpdateService
{
    private readonly ILogger<LeagueUpdateService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;
    private readonly LeagueService _leagueService;
    private readonly ApiService _apiService;

    public LeagueUpdateService(
        ILogger<LeagueUpdateService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory,
        LeagueService leagueService,
        ApiService apiService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _leagueService = leagueService;
        _apiService = apiService;
    }

    public async Task UpdateInfo()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var leagues = await context.Set<League>()
                .Include(l => l.Site)
                .Include(l => l.LeagueTeams)
                .Where(l => l.Enabled && l.Site.Enabled)
                .ToListAsync();

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<ILeagueInfoApi>(league);

                if (api != null)
                {
                    var data = await api.GetLeagueInfoAsync(league);

                    if (data != null)
                    {
                        league.Name = data.Name;
                        league.Info = data.Info;
                    }
                }
            }));

            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateTeams()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var teams = (await context.Set<Team>().ToListAsync())
                .ToDictionary(t => CleanTeamName(t.Name));

            var leagues = await context.Set<League>()
                .Include(l => l.Site)
                .Include(l => l.LeagueTeams)
                .Where(l => l.Enabled && l.Site.Enabled)
                .ToListAsync();

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<ITeamsApi>(league);

                if (api != null)
                {
                    var data = await api.GetTeamsAsync(league);

                    if (data?.Count() > 0)
                    {
                        foreach (var lt in data)
                        {
                            var key = CleanTeamName(lt.Team.Name);

                            if (!teams.ContainsKey(key))
                                continue;

                            lt.TeamId = teams[key]?.Id ?? lt.TeamId;
                            lt.Team = teams[key] ?? lt.Team;
                        }

                        league.LeagueTeams = data;
                    }
                }
            }));

            await context.SaveChangesAsync();
        }
    }

    private string CleanTeamName(string name) =>
        Regex.Replace(name, @"[^0-9a-zA-Z]", "");
}