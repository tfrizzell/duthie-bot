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
                            var from = FindTeam(league, trade.FromId);
                            var to = FindTeam(league, trade.ToId);

                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { from.Id, to.Id },
                                types: new WatcherType[] { WatcherType.Trades }
                            )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                            if (watchers.Count() > 0)
                            {
                                var assets = Regex.Replace(string.Join(", ", trade.Assets), @",([^,]+)$", @", and $1");
                                var timestamp = DateTimeOffset.UtcNow;
                                var url = api.GetTradeUrl(league, trade);

                                var message = league.HasPluralTeamNames()
                                    ? $"The **{MessageUtils.Escape(from.Name)}** have traded **{MessageUtils.Escape(assets)}** to the **{MessageUtils.Escape(to.Name)}**!"
                                    : $"**{MessageUtils.Escape(from.Name)}** has traded **{MessageUtils.Escape(assets)}** to **{MessageUtils.Escape(to.Name)}**!";

                                await _guildMessageService.SaveAsync(watchers.Select(watcher =>
                                    new GuildMessage
                                    {
                                        GuildId = watcher.Key.GuildId,
                                        ChannelId = watcher.Key.ChannelId,
                                        Message = "",
                                        Embed = new GuildMessageEmbed
                                        {
                                            Color = Color.Orange,
                                            Title = $"{league.ShortName} Trade",
                                            Thumbnail = league.LogoUrl,
                                            Content = message,
                                            Timestamp = timestamp,
                                            Url = url,
                                        }
                                    }));
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"Failed to map teams for trade {trade.GetHash()} in league {league.Id}");
                        }

                        league.State.LastTrade = trade.GetHash();
                    }
                }
                else
                    league.State.LastTrade = data?.LastOrDefault()?.GetHash() ?? "";

                await _leagueService.SaveAsync(league);
            }));

            sw.Stop();
            _logger.LogTrace($"Trade tracking task completed in {sw.Elapsed.TotalMilliseconds}ms");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Trade tracking task failed in {sw.Elapsed.TotalMilliseconds}ms");
            _logger.LogError(e, "An unexpected error during trade tracking task.");
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