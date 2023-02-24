using System.Diagnostics;
using Discord;
using Duthie.Bot.Extensions;
using Duthie.Bot.Utils;
using Duthie.Services.Api;
using Duthie.Services.Games;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Watchers;
using Duthie.Types.Modules.Api;
using Duthie.Types.Guilds;
using Duthie.Types.Watchers;
using Microsoft.Extensions.Logging;
using Game = Duthie.Types.Games.Game;

namespace Duthie.Bot.Background;

public class GameBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<GameBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly GameService _gameService;
    private readonly WatcherService _watcherService;
    private readonly GuildMessageService _guildMessageService;

    public GameBackgroundService(
        ILogger<GameBackgroundService> logger,
        ApiService apiService,
        LeagueService leagueService,
        GameService gameService,
        WatcherService watcherService,
        GuildMessageService guildMessageService) : base(logger)
    {
        _logger = logger;
        _apiService = apiService;
        _leagueService = leagueService;
        _gameService = gameService;
        _watcherService = watcherService;
        _guildMessageService = guildMessageService;
    }

    protected override string[] Schedules
    {
        get => new string[]
        {
            "*/30 0-19  * * *",
            "*/5  20-23 * * *",
        };
    }

    public override async Task ExecuteAsync(CancellationToken? cancellationToken = null)
    {
        _logger.LogTrace("Starting game update task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();
            var teamLookup = new TeamLookup(leagues);

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<IGameApi>(league);

                if (api == null)
                    return;

                var data = (await api.GetGamesAsync(league))?
                    .OrderBy(g => g.Timestamp)
                        .ThenBy(g => g.Id);

                if (data == null)
                    return;

                foreach (var game in data)
                {
                    try
                    {
                        var _game = await _gameService.GetByGameIdAsync(game.LeagueId, game.Id);
                        var visitorTeam = teamLookup.Get(league, game.VisitorId);
                        var homeTeam = teamLookup.Get(league, game.HomeId);

                        if (_game != null && _game.Timestamp == game.Timestamp && _game.VisitorId == visitorTeam.Id && _game.VisitorScore == game.VisitorScore && _game.HomeId == homeTeam.Id && _game.HomeScore == game.HomeScore && _game.Overtime == game.Overtime && _game.Shootout == game.Shootout)
                        {
                            continue;
                        }

                        var messages = new List<GuildMessage>();

                        if (ShouldNotify(_game, game))
                        {
                            var url = api.GetGameUrl(league, game);

                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { visitorTeam.Id, homeTeam.Id },
                                types: new WatcherType[] { WatcherType.Games }
                            )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                            foreach (var watcher in watchers)
                            {
                                var message = league.HasPluralTeamNames()
                                    ? "The {us} have {outcome} the {them} by the score of {score}"
                                    : "{us} has {outcome} {them} by the score of {score}";

                                var (us, usScore) = watcher.Any(w => w.TeamId == homeTeam.Id) ? (homeTeam, game.HomeScore) : (visitorTeam, game.VisitorScore);
                                var (them, themScore) = watcher.Any(w => w.TeamId == homeTeam.Id) ? (visitorTeam, game.VisitorScore) : (homeTeam, game.HomeScore);

                                if (usScore > themScore)
                                {
                                    message = message
                                        .Replace("{us}", $"**{MessageUtils.Escape(us.Name)}**")
                                        .Replace("{outcome}", "defeated")
                                        .Replace("{them}", $"**{MessageUtils.Escape(them.Name)}**")
                                        .Replace("{score}", $"**{usScore} to {themScore}**");
                                }
                                else if (usScore < themScore)
                                {
                                    message = message
                                        .Replace("{us}", $"*{MessageUtils.Escape(us.Name)}*")
                                        .Replace("{outcome}", "been defeated by")
                                        .Replace("{them}", $"*{MessageUtils.Escape(them.Name)}*")
                                        .Replace("{score}", $"*{themScore} to {usScore}*");
                                }
                                else
                                {
                                    message = message
                                        .Replace("{us}", MessageUtils.Escape(us.Name))
                                        .Replace("{outcome}", "tied")
                                        .Replace("{them}", MessageUtils.Escape(them.Name))
                                        .Replace("{score}", $"{usScore} to {themScore}");
                                }

                                if (game.Shootout == true)
                                    message += " in a shootout";
                                else if (game.Overtime == true)
                                    message += " in overtime";

                                messages.Add(new GuildMessage
                                {
                                    GuildId = watcher.Key.GuildId,
                                    ChannelId = watcher.Key.ChannelId,
                                    Color = usScore > themScore
                                        ? Color.DarkGreen
                                        : (usScore < themScore
                                            ? Color.DarkRed
                                            : Colors.Black),
                                    Title = $"{league.ShortName} Game Result",
                                    Thumbnail = league.LogoUrl,
                                    Content = string.IsNullOrWhiteSpace(url) ? message : $"{message}\n\n[Box Score]({url})",
                                    Url = url,
                                });
                            }
                        }

                        await Task.WhenAll(
                            _gameService.SaveAsync(new Game
                            {
                                Id = _game?.Id ?? Guid.Empty,
                                LeagueId = league.Id,
                                GameId = game.Id,
                                Timestamp = game.Timestamp,
                                VisitorId = visitorTeam.Id,
                                VisitorScore = game.VisitorScore,
                                HomeId = homeTeam.Id,
                                HomeScore = game.HomeScore,
                                Overtime = game.Overtime,
                                Shootout = game.Shootout,
                            }),
                            messages.Count() == 0 ? Task.CompletedTask : _guildMessageService.SaveAsync(messages));
                    }
                    catch (KeyNotFoundException e)
                    {
                        _logger.LogWarning(e, $"Failed to map teams for game {game.Id} for league \"{league.Name}\" [{league.Id}]");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"An unexpected error has occurred while processing game {game.Id} for league \"{league.Name}\" [{league.Id}]");
                    }
                }
            }));

            sw.Stop();
            _logger.LogTrace($"Game update task completed in {sw.Elapsed.TotalMilliseconds}ms");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Game update task failed in {sw.Elapsed.TotalMilliseconds}ms");
            _logger.LogError(e, "An unexpected error during game update task.");
        }
    }

    private static bool ShouldNotify(Game? prev, Types.Modules.Data.Game next) =>
        prev != null
        && next.VisitorScore != null
        && next.HomeScore != null
        && (
            prev.VisitorScore != next.VisitorScore
            || prev.HomeScore != next.HomeScore
            || prev.Overtime != next.Overtime
            || prev.Shootout != next.Shootout
        );
}