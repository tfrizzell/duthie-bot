using Duthie.Services.Background;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class LeagueInfoBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<LeagueInfoBackgroundService> _logger;
    private readonly LeagueUpdateService _leagueInfoUpdateService;

    public LeagueInfoBackgroundService(
        ILogger<LeagueInfoBackgroundService> logger,
        LeagueUpdateService leagueInfoUpdateService) : base(logger)
    {
        _logger = logger;
        _leagueInfoUpdateService = leagueInfoUpdateService;
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

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogTrace("Updating league information");
            await _leagueInfoUpdateService.UpdateAll();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred while updating league information.");
        }
    }
}