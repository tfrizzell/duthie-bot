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
using Duthie.Types.Teams;
using Duthie.Types.Watchers;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Duthie.Bot.Background;

public class TradeBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<TradeBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly WatcherService _watcherService;
    private readonly GuildMessageService _guildMessageService;

    public TradeBackgroundService(
        ILogger<TradeBackgroundService> logger,
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
        _logger.LogTrace("Starting trade tracking task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<ITradeApi>(league);

                if (api == null)
                    return;

                var data = (await api.GetTradesAsync(league))?
                    .Where(t => t.FromAssets.Count() + t.ToAssets.Count() > 0)
                    .OrderBy(b => b.Timestamp)
                        .ThenBy(b => b.GetHash())
                    .ToList();

                if (data?.Count() > 0 && league.State.LastTrade != null)
                {
                    var lastTradeIndex = data.FindIndex(b => b.GetHash() == league.State.LastTrade);

                    if (lastTradeIndex >= 0)
                        data.RemoveRange(0, lastTradeIndex + 1);

                    foreach (var trade in data)
                    {
                        try
                        {
                            if (trade.FromAssets.Count() == 0)
                                trade.Reverse();

                            var from = FindTeam(league, trade.FromId);
                            var to = FindTeam(league, trade.ToId);

                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { from.Id, to.Id },
                                types: new WatcherType[] { WatcherType.Trades }
                            )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                            if (watchers.Count() > 0)
                            {
                                var timestamp = DateTimeOffset.UtcNow;
                                var url = api.GetTradeUrl(league, trade);

                                var fromAssets = Regex.Replace(string.Join(", ",
                                        trade.FromAssets
                                            .Select(a => league.Teams.Aggregate(a, (a, t) => a.Replace(t.Name, t.ShortName)))
                                            .ToArray()), @", ([^,]+)$", @", and $1");

                                var toAssets = Regex.Replace(string.Join(", ",
                                        trade.ToAssets
                                            .Select(a => league.Teams.Aggregate(a, (a, t) => a.Replace(t.Name, t.ShortName)))
                                            .ToArray()), @", ([^,]+)$", @", and $1");

                                var message = league.HasPluralTeamNames()
                                    ? "The {us} have {action} {usAssets} {direction} the {them} in exchange for {themAssets}!"
                                    : "{us} has {action} {usAssets} {direction} {them} in exchange for {themAssets}!";

                                if (watchers.Any(watcher => watcher.Any(w => w.TeamId == from.Id)))
                                {
                                    message = message
                                        .Replace("{us}", $"**{MessageUtils.Escape(from.Name)}**")
                                        .Replace("{action}", "traded")
                                        .Replace("{usAssets}", fromAssets)
                                        .Replace("{direction}", "to")
                                        .Replace("{them}", $"**{MessageUtils.Escape(to.Name)}**")
                                        .Replace("{themAssets}", toAssets);
                                }
                                else
                                {
                                    message = message
                                        .Replace("{us}", $"**{MessageUtils.Escape(to.Name)}**")
                                        .Replace("{action}", "acquired")
                                        .Replace("{usAssets}", toAssets)
                                        .Replace("{direction}", "from")
                                        .Replace("{them}", $"**{MessageUtils.Escape(from.Name)}**")
                                        .Replace("{themAssets}", fromAssets);
                                }

                                message = Regex.Replace(message, @"\s+ in exchange for\s*!$", "!");

                                await _guildMessageService.SaveAsync(watchers.Select(watcher =>
                                    new GuildMessage
                                    {
                                        GuildId = watcher.Key.GuildId,
                                        ChannelId = watcher.Key.ChannelId,
                                        Message = "",
                                        Embed = new GuildMessageEmbed
                                        {
                                            Color = Color.Blue,
                                            Title = $"{league.ShortName} Trade",
                                            Thumbnail = league.LogoUrl,
                                            Content = message,
                                            Timestamp = timestamp,
                                            Url = url,
                                        }
                                    }));
                            }
                        }
                        catch (KeyNotFoundException e)
                        {
                            _logger.LogWarning(e, $"Failed to map teams for trade {trade.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"An unexpected error has occurred while processing trade {trade.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }

                        league.State.LastTrade = trade.GetHash();
                    }

                    if (data.Count() > 0)
                        _logger.LogTrace($"Successfully processed {data.Count()} new trades for league \"{league.Name}\" [{league.Id}]");
                }
                else
                    league.State.LastTrade = data?.LastOrDefault()?.GetHash() ?? "";

                await _leagueService.SaveStateAsync(league.Id, LeagueStateType.Trade, league.State.LastTrade);
            }));

            sw.Stop();
            _logger.LogTrace($"Trade tracking task completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Trade tracking task failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during trade tracking task.");
        }
    }

    private static Team FindTeam(League league, string externalId)
    {
        var team = league.LeagueTeams.FirstOrDefault(t => t.ExternalId == externalId);

        if (team == null)
            throw new KeyNotFoundException($"no team with external id {externalId} was found for league \"{league.Name}\" [{league.Id}]");

        return team.Team;
    }
}