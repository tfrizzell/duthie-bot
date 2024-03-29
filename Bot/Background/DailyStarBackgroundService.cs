using System.Diagnostics;
using Duthie.Bot.Utils;
using Duthie.Services.Api;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Watchers;
using Duthie.Types.Modules.Api;
using Duthie.Types.Guilds;
using Duthie.Types.Leagues;
using Duthie.Types.Watchers;
using Microsoft.Extensions.Logging;
using Duthie.Bot.Extensions;
using System.Text.RegularExpressions;

namespace Duthie.Bot.Background;

public class DailyStarBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<DailyStarBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly WatcherService _watcherService;
    private readonly GuildMessageService _guildMessageService;

    public DailyStarBackgroundService(
        ILogger<DailyStarBackgroundService> logger,
        ApiService apiService,
        LeagueService leagueService,
        WatcherService watcherService,
        GuildMessageService guildMessageService) : base(logger)
    {
        _logger = logger;
        _apiService = apiService;
        _leagueService = leagueService;
        _watcherService = watcherService;
        _guildMessageService = guildMessageService;
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
        _logger.LogTrace("Starting daily star tracking task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();
            leagues = leagues.Where(l => l.State.LastDailyStarTimestamp == null || (DateTimeOffset.UtcNow - l.State.LastDailyStarTimestamp.GetValueOrDefault()).TotalHours >= 18);

            var teamLookup = new TeamLookup(leagues);

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<IDailyStarApi>(league);

                if (api == null)
                    return;

                var data = (await api.GetDailyStarsAsync(league))?
                    .OrderBy(s => s.Timestamp)
                        .ThenBy(s => s.Position == "Forward" ? 1 : s.Position == "Defense" ? 2 : 3)
                        .ThenBy(s => s.Rank)
                    .ToList();

                if (data?.Count() > 0 && league.State.LastDailyStarTimestamp != null)
                {
                    try
                    {
                        var teams = data.DistinctBy(s => s.TeamId)
                            .ToDictionary(s => s.TeamId, s => teamLookup.Get(league, s.TeamId));

                        var watchers = (await _watcherService.FindAsync(
                            leagues: new Guid[] { league.Id },
                            teams: teams.Values.Select(t => t.Id),
                            types: new WatcherType[] { WatcherType.DailyStars }
                        )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                        if (watchers.Count() > 0)
                        {
                            await _guildMessageService.SaveAsync(watchers.Select(watcher =>
                            {
                                var stars = data.Where(s => watcher.Any(w => w.TeamId == teams[s.TeamId].Id));

                                if (stars.Count() == 0)
                                    return null;

                                var url = api.GetDailyStarsUrl(league, stars.First());

                                return new GuildMessage
                                {
                                    GuildId = watcher.Key.GuildId,
                                    ChannelId = watcher.Key.ChannelId,
                                    Color = Colors.Chocolate,
                                    Title = $"{league.ShortName} Daily Stars",
                                    Thumbnail = league.LogoUrl,
                                    Content = string.Join("\n\n",
                                        new string[] { $"**Congratulations to today's {MessageUtils.Escape(league.Name)} daily stars!**" }
                                        .Concat(stars.GroupBy(star => Regex.Replace($"{MessageUtils.Escape(star.Position)}s", @"ses$", "se"))
                                            .Select(star => string.Join("\n  ",
                                                new string[] { $"{star.Key}:" }
                                                .Concat(star.Select(s => $"{s.Rank.Ordinal()} Star - {MessageUtils.Escape(s.PlayerName)} | `{s.GetStatLine()}`")))))),
                                    Url = url,
                                };
                            })
                            .Where(m => m != null)
                            .Cast<GuildMessage>());
                        }
                    }
                    catch (KeyNotFoundException e)
                    {
                        _logger.LogWarning(e, $"Failed to map teams for daily stars for league \"{league.Name}\" [{league.Id}]");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"An unexpected error has occurred while processing daily stars for league \"{league.Name}\" [{league.Id}]");
                    }

                    league.State.LastDailyStarTimestamp = DateTimeOffset.UtcNow;

                    if (data.Count() > 0)
                        _logger.LogTrace($"Successfully processed {MessageUtils.Pluralize(data.Count(), "new daily star")} for league \"{league.Name}\" [{league.Id}]");
                }
                else if (league.State.LastDailyStarTimestamp == null)
                    league.State.LastDailyStarTimestamp = DateTimeOffset.UtcNow;

                await _leagueService.SaveStateAsync(league, LeagueStateType.DailyStar);
            }));

            sw.Stop();
            _logger.LogTrace($"Daily star tracking task completed in {sw.Elapsed.TotalMilliseconds}ms");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Daily star tracking task failed in {sw.Elapsed.TotalMilliseconds}ms");
            _logger.LogError(e, "An unexpected error during daily star tracking task.");
        }
    }
}