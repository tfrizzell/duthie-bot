using System.Diagnostics;
using System.Reflection;
using Duthie.Services.Api;
using Duthie.Services.Games;
using Duthie.Services.Leagues;
using Duthie.Types.Modules.Api;
using Duthie.Types.Leagues;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class LeagueBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<LeagueBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly GameService _gameService;

    public LeagueBackgroundService(
        ILogger<LeagueBackgroundService> logger,
        ApiService apiService,
        LeagueService leagueService,
        GameService gameService) : base(logger)
    {
        _logger = logger;
        _apiService = apiService;
        _leagueService = leagueService;
        _gameService = gameService;
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

            var seasonChanges = (await Task.WhenAll(leagues.Select(async league =>
            {
                try
                {
                    var api = _apiService.Get<ILeagueApi>(league);

                    if (api == null)
                        return null;

                    var data = await api.GetLeagueAsync(league);

                    if (data == null)
                        return null;

                    league.Name = data.Name;
                    league.LogoUrl = data.LogoUrl;
                    league.Info = data.Info;

                    var seasonIdProp = league.Info?.GetType().GetProperty("SeasonId", BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if (seasonIdProp != null)
                    {
                        var prevSeasonId = seasonIdProp.GetValue(league.Info);
                        var nextSeasonId = seasonIdProp.GetValue(data.Info);

                        if (!Object.Equals(prevSeasonId, nextSeasonId))
                        {
                            return new SeasonChange
                            {
                                League = league,
                                PrevSeasonId = prevSeasonId,
                                NextSeasonId = nextSeasonId
                            };
                        }
                    }
                    else
                        return null;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"An unexpected error has occurred while updating league \"{league.Name}\" [{league.Id}]");
                }

                return null;
            })))
            .Where(l => l != null)
            .Cast<SeasonChange>();

            await _leagueService.SaveAsync(leagues);

            foreach (var change in seasonChanges)
            {
                _logger.LogDebug($"League \"{change.League.Name}\" [{change.League.Id}] has changed seasons from {change.PrevSeasonId} to {change.NextSeasonId}");
                var games = await _gameService.GetAllAsync(change.League.Id);

                if (games.Count() > 0)
                {
                    await _gameService.DeleteAsync(games.Select(g => g.Id));
                    _logger.LogTrace($"Deleted {games.Count()} games for league \"{change.League.Name}\" [{change.League.Id}]");
                }
            }

            sw.Stop();
            _logger.LogTrace($"League update task completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"League update task failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during league update task.");
        }
    }

    private class SeasonChange
    {
        internal League League { get; set; } = default!;
        internal object? PrevSeasonId { get; set; }
        internal object? NextSeasonId { get; set; }
    }
}