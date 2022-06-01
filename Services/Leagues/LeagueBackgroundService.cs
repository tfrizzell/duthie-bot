using Duthie.Bot.Services;
using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Services.Leagues;
using Duthie.Types;
using Duthie.Types.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Background;

public class LeagueBackgroundService
{
    private readonly ILogger<LeagueBackgroundService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;
    private readonly LeagueService _leagueService;
    private readonly ApiService _apiService;

    public LeagueBackgroundService(
        ILogger<LeagueBackgroundService> logger,
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
            var leagues = await context.Set<League>().Where(l => l.Enabled).ToListAsync();

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<ILeagueInfoApi>(league);

                if (api != null)
                {
                    var data = await api.GetLeagueInfoAsync(league);
                    league.Name = data.Name;
                    league.Info = data.Info;
                    await context.Set<League>().UpdateAsync(league);
                }
            }));

            await context.SaveChangesAsync();
        }
    }
}