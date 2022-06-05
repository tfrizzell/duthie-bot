using System.Diagnostics;
using Duthie.Services.Api;
using Duthie.Services.Leagues;
using Duthie.Types.Api;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class LeagueBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<LeagueBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;

    public LeagueBackgroundService(
        ILogger<LeagueBackgroundService> logger,
        ApiService apiService,
        LeagueService leagueService) : base(logger)
    {
        _logger = logger;
        _apiService = apiService;
        _leagueService = leagueService;
    }

    protected override string[] Schedules
    {
        get => new string[]
        {
            "0 */12 * * *"
        };
    }

    public override async Task ExecuteAsync(CancellationToken? cancellationToken = null)
    {
        _logger.LogTrace("Starting league update task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();

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

            await _leagueService.SaveAsync(leagues);

            sw.Stop();
            _logger.LogTrace($"League update task completed in {sw.Elapsed.TotalMilliseconds}ms");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"League update task failed in {sw.Elapsed.TotalMilliseconds}ms");
            _logger.LogError(e, "An unexpected error during league update task.");
        }
    }
}