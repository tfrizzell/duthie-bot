using Duthie.Services.Background;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class LeagueTeamBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<LeagueTeamBackgroundService> _logger;
    private readonly LeagueUpdateService _leagueUpdateService;

    public LeagueTeamBackgroundService(
        ILogger<LeagueTeamBackgroundService> logger,
        LeagueUpdateService leagueUpdateService) : base(logger)
    {
        _logger = logger;
        _leagueUpdateService = leagueUpdateService;
    }

    protected override string[] Schedules
    {
        get => new string[]
        {
            "30 */12 * * *"
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
            _logger.LogTrace("Updating league teams");
            await _leagueUpdateService.UpdateTeams();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred while updating league teams.");
        }
    }
}