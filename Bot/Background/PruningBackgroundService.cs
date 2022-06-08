using System.Diagnostics;
using Duthie.Services.Api;
using Duthie.Services.Guilds;
using Duthie.Services.Watchers;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class PruningBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<PruningBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly GuildMessageService _guildMessageService;
    private readonly GuildService _guildService;
    private readonly WatcherService _watcherService;

    public PruningBackgroundService(
        ILogger<PruningBackgroundService> logger,
        ApiService apiService,
        GuildMessageService guildMessageService,
        GuildService guildService,
        WatcherService watcherService) : base(logger)
    {
        _logger = logger;
        _apiService = apiService;
        _guildMessageService = guildMessageService;
        _guildService = guildService;
        _watcherService = watcherService;
    }

    protected override string[] Schedules
    {
        get => new string[]
        {
            "0 * * * *",
        };
    }

    public override async Task ExecuteAsync(CancellationToken? cancellationToken = null)
    {
        _logger.LogTrace("Starting data pruning task");
        var sw = Stopwatch.StartNew();

        try
        {
            await Task.WhenAll(
                _guildMessageService.PruneAsync(),
                _guildService.PruneAsync(),
                _watcherService.PruneAsync());

            sw.Stop();
            _logger.LogTrace($"Data pruning task completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Data pruning task failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during data pruning task.");
        }
    }
}