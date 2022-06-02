using Duthie.Data;
using Duthie.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class LeagueUpdateBackgroundService : IHostedService, IDisposable
{
    private readonly ILogger<LeagueUpdateBackgroundService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;
    private readonly LeagueUpdateService _leagueInfoUpdateService;
    private Timer? _timer;

    public LeagueUpdateBackgroundService(
        ILogger<LeagueUpdateBackgroundService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory,
        LeagueUpdateService leagueInfoUpdateService)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _leagueInfoUpdateService = leagueInfoUpdateService;
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug($"Starting {GetType().Name}");
        UpdateLeagueInfo();

        var initial = DateTimeOffset.UtcNow;

        if (initial.Hour >= 16)
            initial = initial.Date.AddDays(1).AddHours(4);
        else if (initial.Hour >= 4)
            initial = initial.Date.AddHours(16);
        else
            initial = initial.Date.AddHours(4);

        _timer = new Timer(UpdateLeagueInfo, null, initial - DateTimeOffset.UtcNow, TimeSpan.FromHours(12));
        return Task.CompletedTask;
    }

    private async void UpdateLeagueInfo(object? state = null)
    {
        try
        {
            using (var context = await _contextFactory.CreateDbContextAsync())
            {
                _logger.LogDebug("Updating league information");
                await _leagueInfoUpdateService.UpdateInfo();

                _logger.LogDebug("Updating league teams");
                await _leagueInfoUpdateService.UpdateTeams();
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An unexpected error occurred while updating league information.");
        }
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogDebug($"Stopping {GetType().Name}");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}