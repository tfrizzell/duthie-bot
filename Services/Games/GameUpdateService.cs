using System.Text.RegularExpressions;
using Duthie.Bot.Services;
using Duthie.Services.Games;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Watchers;
using Duthie.Types.Api;
using Duthie.Types.Guilds;
using Duthie.Types.Leagues;
using Duthie.Types.Watchers;
using Microsoft.Extensions.Logging;
using Game = Duthie.Types.Games.Game;

namespace Duthie.Services.Background;

public class GameUpdateService
{
    private readonly ILogger<GameUpdateService> _logger;
    private readonly LeagueService _leagueService;
    private readonly GameService _gameService;
    private readonly WatcherService _watcherService;
    private readonly GuildMessageService _guildMessageService;
    private readonly ApiService _apiService;

    public GameUpdateService(
        ILogger<GameUpdateService> logger,
        LeagueService leagueService,
        GameService gameService,
        WatcherService watcherService,
        GuildMessageService guildMessageService,
        ApiService apiService)
    {
        _logger = logger;
        _leagueService = leagueService;
        _gameService = gameService;
        _watcherService = watcherService;
        _guildMessageService = guildMessageService;
        _apiService = apiService;
    }

    public async Task UpdateGames()
    {
        _logger.LogInformation("Updating games");

        var leagues = await _leagueService.GetAllAsync();
        var games = new List<Game>();
        var messages = new List<GuildMessage>();

        await Task.WhenAll(leagues.Select(async league =>
        {
            var api = _apiService.Get<IGamesApi>(league);

            if (api == null)
                return;

            var data = await api.GetGamesAsync(league);

            if (data == null)
                return;

            foreach (var game in data)
            {
                try
                {
                    var g = await _gameService.GetByGameIdAsync(game.LeagueId, game.GameId);
                    var visitorTeam = FindTeam(league, game.VisitorExternalId);
                    var homeTeam = FindTeam(league, game.HomeExternalId);

                    if (g == null)
                    {
                        games.Add(new Game
                        {
                            LeagueId = league.Id,
                            GameId = game.GameId,
                            Timestamp = game.Timestamp,
                            VisitorId = visitorTeam.TeamId,
                            VisitorScore = game.VisitorScore,
                            HomeId = homeTeam.TeamId,
                            HomeScore = game.HomeScore,
                            Overtime = game.Overtime,
                            Shootout = game.Shootout,
                        });
                    }
                    else if (g.Timestamp != game.Timestamp || g.VisitorId != visitorTeam.TeamId || g.VisitorScore != game.VisitorScore
                        || g.HomeId != homeTeam.TeamId || g.HomeScore != game.HomeScore || g.Overtime != game.Overtime || g.Shootout != game.Shootout)
                    {
                        games.Add(new Game
                        {
                            Id = g.Id,
                            LeagueId = league.Id,
                            GameId = game.GameId,
                            Timestamp = game.Timestamp,
                            VisitorId = visitorTeam.TeamId,
                            VisitorScore = game.VisitorScore,
                            HomeId = homeTeam.TeamId,
                            HomeScore = game.HomeScore,
                            Overtime = game.Overtime,
                            Shootout = game.Shootout,
                        });

                        if ((g.VisitorScore == game.VisitorScore && g.HomeScore == game.HomeScore) || game.VisitorScore == null || game.VisitorScore < 0 || game.HomeScore == null || game.HomeScore < 0)
                            return;

                        var watchers = (await _watcherService.FindAsync(
                            leagues: new Guid[] { league.Id },
                            teams: new Guid[] { visitorTeam.TeamId, homeTeam.TeamId },
                            types: new WatcherType[] { WatcherType.Games }
                        )).GroupBy(w => new { w.GuildId, w.ChannelId });

                        if (watchers.Count() == 0)
                            return;

                        foreach (var watcher in watchers)
                        {
                            var message = league.Tags.Intersect(new string[] { "esports", "tournament", "pickup", "club teams" }).Count() > 0
                                ? "{us} has {outcome}  {them} by the score of {score}"
                                : "The {us} have {outcome} the {them} by the score of {score}";

                            var (us, usScore) = watcher.Any(w => w.TeamId == homeTeam.TeamId) ? (homeTeam.Team, game.HomeScore) : (visitorTeam.Team, game.VisitorScore);
                            var (them, themScore) = watcher.Any(w => w.TeamId == homeTeam.TeamId) ? (visitorTeam.Team, game.VisitorScore) : (homeTeam.Team, game.HomeScore);

                            if (usScore > themScore)
                            {
                                message = message
                                    .Replace("{us}", $"**{Escape(us.Name)}**")
                                    .Replace("{outcome}", "defeated")
                                    .Replace("{them}", $"**{Escape(them.Name)}**")
                                    .Replace("{score}", $"**{usScore} to {themScore}**!");
                            }
                            else if (usScore < themScore)
                            {
                                message = message
                                    .Replace("{us}", $"*{Escape(us.Name)}*")
                                    .Replace("{outcome}", "been defeated by")
                                    .Replace("{them}", $"*{Escape(them.Name)}*")
                                    .Replace("{score}", $"*{themScore} to {usScore}*.");
                            }
                            else
                            {
                                message = message
                                    .Replace("{us}", Escape(us.Name))
                                    .Replace("{outcome}", "tied")
                                    .Replace("{them}", Escape(them.Name))
                                    .Replace("{score}", $"{usScore} to {themScore}.");
                            }

                            if (game.Shootout == true)
                                message = Regex.Replace(message, @"[!.]$", @" in a shootout$1");
                            else if (game.Overtime == true)
                                message = Regex.Replace(message, @"[!.]$", @" in overtime$1");

                            messages.Add(new GuildMessage
                            {
                                GuildId = watcher.Key.GuildId,
                                ChannelId = watcher.Key.ChannelId ?? 0,
                                Message = message
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Failed to map teams for game {game.GameId} in league {league.Id}");
                }
            }
        }));

        await _gameService.SaveAsync(games);
        await _guildMessageService.SaveAsync(messages);
    }

    private static LeagueTeam FindTeam(League league, string externalId)
    {
        var team = league.LeagueTeams.FirstOrDefault(t => t.ExternalId == externalId);

        if (team == null)
            throw new KeyNotFoundException($"no team with external id {externalId} was found for league {league.Id}");

        return team;
    }

    private static string Escape(string text) =>
        Regex.Replace(text, @"[*_~`]", @"\\$1");
}