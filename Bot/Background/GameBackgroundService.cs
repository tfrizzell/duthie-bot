using System.Diagnostics;
using System.Text.RegularExpressions;
using Discord;
using Duthie.Bot.Extensions;
using Duthie.Bot.Utils;
using Duthie.Services.Api;
using Duthie.Services.Games;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Watchers;
using Duthie.Types.Api;
using Duthie.Types.Guilds;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;
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

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<IGameApi>(league);

                if (api == null)
                    return;

                var data = await api.GetGamesAsync(league);

                if (data == null)
                    return;

                foreach (var game in data)
                {
                    try
                    {
                        var _game = await _gameService.GetByGameIdAsync(game.LeagueId, game.GameId);
                        var visitorTeam = FindTeam(league, game.VisitorExternalId);
                        var homeTeam = FindTeam(league, game.HomeExternalId);

                        if (_game != null && _game.Timestamp == game.Timestamp && _game.VisitorId == visitorTeam.Id && _game.VisitorScore == game.VisitorScore && _game.HomeId == homeTeam.Id && _game.HomeScore == game.HomeScore && _game.Overtime == game.Overtime && _game.Shootout == game.Shootout)
                        {
                            continue;
                        }

                        var messages = new List<GuildMessage>();

                        if (_game != null && game.VisitorScore != null && game.HomeScore != null)
                        {
                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { visitorTeam.Id, homeTeam.Id },
                                types: new WatcherType[] { WatcherType.Games }
                            )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                            foreach (var watcher in watchers)
                            {
                                var message = league.HasPluralTeamNames()
                                ? $"The {{us}} have {{outcome}} the {{them}} by the score of {{score}}"
                                : $"{{us}} has {{outcome}} {{them}} by the score of {{score}}";
                                var (us, usScore) = watcher.Any(w => w.TeamId == homeTeam.Id) ? (homeTeam, game.HomeScore) : (visitorTeam, game.VisitorScore);
                                var (them, themScore) = watcher.Any(w => w.TeamId == homeTeam.Id) ? (visitorTeam, game.VisitorScore) : (homeTeam, game.HomeScore);

                                if (usScore > themScore)
                                {
                                    message = message
                                        .Replace("{us}", $"**{MessageUtils.Escape(us.Name)}**")
                                        .Replace("{outcome}", "defeated")
                                        .Replace("{them}", $"**{MessageUtils.Escape(them.Name)}**")
                                        .Replace("{score}", $"**{usScore} to {themScore}**!");
                                }
                                else if (usScore < themScore)
                                {
                                    message = message
                                        .Replace("{us}", $"*{MessageUtils.Escape(us.Name)}*")
                                        .Replace("{outcome}", "been defeated by")
                                        .Replace("{them}", $"*{MessageUtils.Escape(them.Name)}*")
                                        .Replace("{score}", $"*{themScore} to {usScore}*.");
                                }
                                else
                                {
                                    message = message
                                        .Replace("{us}", MessageUtils.Escape(us.Name))
                                        .Replace("{outcome}", "tied")
                                        .Replace("{them}", MessageUtils.Escape(them.Name))
                                        .Replace("{score}", $"{usScore} to {themScore}.");
                                }

                                if (game.Shootout == true)
                                    message = Regex.Replace(message, @"[!.]$", @" in a shootout$0");
                                else if (game.Overtime == true)
                                    message = Regex.Replace(message, @"[!.]$", @" in overtime$0");

                                var url = api.GetGameUrl(league, game);

                                messages.Add(new GuildMessage
                                {
                                    GuildId = watcher.Key.GuildId,
                                    ChannelId = watcher.Key.ChannelId,
                                    Message = "",
                                    Embed = new GuildMessageEmbed
                                    {
                                        ShowAuthor = false,
                                        Color = usScore > themScore
                                            ? Color.DarkGreen
                                            : (usScore < themScore
                                                ? Color.DarkRed
                                                : 0),
                                        Title = $"{league.ShortName} Game Result",
                                        Thumbnail = league.LogoUrl,
                                        Content = string.IsNullOrWhiteSpace(url) ? message : $"{message}\n\n[Box Score]({url})",
                                        Timestamp = DateTimeOffset.UtcNow,
                                        Url = url,
                                    }
                                });
                            }
                        }

                        await Task.WhenAll(
                            _gameService.SaveAsync(new Game
                            {
                                Id = _game?.Id ?? game.Id,
                                LeagueId = league.Id,
                                GameId = game.GameId,
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
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Failed to map teams for game {game.GameId} in league {league.Id}");
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

    private static Team FindTeam(League league, string externalId)
    {
        var team = league.LeagueTeams.FirstOrDefault(t => t.ExternalId == externalId);

        if (team == null)
            throw new KeyNotFoundException($"no team with external id {externalId} was found for league {league.Id}");

        return team.Team;
    }
}