using System.Diagnostics;
using Discord;
using Duthie.Bot.Extensions;
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

namespace Duthie.Bot.Background;

public class NewsBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<NewsBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly WatcherService _watcherService;
    private readonly GuildMessageService _guildMessageService;

    public NewsBackgroundService(
        ILogger<NewsBackgroundService> logger,
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
            "*/15 * * * *",
        };
    }

    public override async Task ExecuteAsync(CancellationToken? cancellationToken = null)
    {
        _logger.LogTrace("Starting news tracking task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();
            var teamLookup = new TeamLookup(leagues);

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<INewsApi>(league);

                if (api == null)
                    return;

                var data = (await api.GetNewsAsync(league))?
                    .Where(n => league.State.LastNewsItemTimestamp == null || n.Timestamp >= league.State.LastNewsItemTimestamp)
                    .OrderBy(n => n.Timestamp)
                    .ToList();

                if (data?.Count() > 0 && league.State.LastNewsItemHash != null)
                {
                    var lastNewsIndex = data.FindIndex(b => b.GetHash() == league.State.LastNewsItemHash);

                    if (lastNewsIndex >= 0)
                        data.RemoveRange(0, lastNewsIndex + 1);

                    foreach (var news in data)
                    {
                        try
                        {
                            var team = teamLookup.Get(league, news.TeamId);

                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { team.Id },
                                types: new WatcherType[] { WatcherType.News }
                            )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                            if (watchers.Count() > 0)
                            {
                                var url = api.GetNewsUrl(league, news);

                                await _guildMessageService.SaveAsync(watchers.Select(watcher =>
                                    new GuildMessage
                                    {
                                        GuildId = watcher.Key.GuildId,
                                        ChannelId = watcher.Key.ChannelId,
                                        Color = Color.Orange,
                                        Title = $"{league.ShortName} News",
                                        Thumbnail = league.LogoUrl,
                                        Content = news.Message,
                                        Url = url,
                                        Timestamp = news.Timestamp,
                                    }));
                            }
                        }
                        catch (KeyNotFoundException e)
                        {
                            _logger.LogWarning(e, $"Failed to map teams for news {news.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"An unexpected error has occurred while processing news {news.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }

                        league.State.LastNewsItemHash = news.GetHash();
                        league.State.LastNewsItemTimestamp = news.Timestamp;
                    }

                    if (data.Count() > 0)
                        _logger.LogTrace($"Successfully processed {MessageUtils.Pluralize(data.Count(), "new winning news")} for league \"{league.Name}\" [{league.Id}]");
                }
                else if (league.State.LastNewsItemHash == null)
                {
                    var lastNewsItem = data?.LastOrDefault();
                    league.State.LastNewsItemHash = lastNewsItem?.GetHash() ?? "";
                    league.State.LastNewsItemTimestamp = lastNewsItem?.Timestamp;
                }

                await _leagueService.SaveStateAsync(league, LeagueStateType.News);
            }));

            sw.Stop();
            _logger.LogTrace($"News tracking task completed in {sw.Elapsed.TotalMilliseconds}ms");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"News tracking task failed in {sw.Elapsed.TotalMilliseconds}ms");
            _logger.LogError(e, "An unexpected error during news tracking task.");
        }
    }
}