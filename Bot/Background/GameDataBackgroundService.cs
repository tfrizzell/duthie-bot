using Duthie.Services.Background;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class GameDataBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<GameDataBackgroundService> _logger;
    private readonly GameUpdateService _gameUpdatingService;

    public GameDataBackgroundService(
        ILogger<GameDataBackgroundService> logger,
        GameUpdateService gameUpdatingService) : base(logger)
    {
        _logger = logger;
        _gameUpdatingService = gameUpdatingService;
    }

    protected override string[] Schedules
    {
        get => new string[]
        {
            "*/30 0-19  * * *",
            "*/5  20-23 * * *",
            "50 * * * *",
        };
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogTrace("Updating game data");
            await _gameUpdatingService.UpdateGames();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred while updating game data.");
        }
    }
}