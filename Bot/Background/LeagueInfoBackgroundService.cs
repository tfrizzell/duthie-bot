using Duthie.Services.Api;
using Duthie.Services.Leagues;
using Duthie.Types.Api;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class LeagueInfoBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<LeagueInfoBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;

    public LeagueInfoBackgroundService(
        ILogger<LeagueInfoBackgroundService> logger,
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

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting {GetType().Name}");

        await Task.WhenAll(
            ExecuteAsync(cancellationToken),
            ScheduleAsync(cancellationToken));
    }

    public override async Task ExecuteAsync(CancellationToken? cancellationToken = null)
    {
        try
        {
            _logger.LogInformation("Updating league information");
            var leagues = await _leagueService.GetAllAsync();

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<ILeagueInfoApi>(league);

                if (api == null)
                    return;

                var info = await api.GetLeagueInfoAsync(league);

                if (info == null)
                    return;

                league.Name = info.Name;
                league.Info = info.Info;
            }));

            await _leagueService.SaveAsync(leagues);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred while updating league information.");
        }
    }
}