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

public class BidBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<BidBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly WatcherService _watcherService;
    private readonly GuildMessageService _guildMessageService;

    public BidBackgroundService(
        ILogger<BidBackgroundService> logger,
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
        _logger.LogTrace("Starting bid tracking task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();
            var teamLookup = new TeamLookup(leagues);

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<IBidApi>(league);

                if (api == null)
                    return;

                var data = (await api.GetBidsAsync(league))?
                    .OrderBy(b => b.Timestamp)
                        .ThenBy(b => b.GetHash())
                    .ToList();

                if (data?.Count() > 0 && league.State.LastBid != null)
                {
                    var lastBidIndex = data.FindIndex(b => b.GetHash() == league.State.LastBid);

                    if (lastBidIndex >= 0)
                        data.RemoveRange(0, lastBidIndex + 1);

                    foreach (var bid in data)
                    {
                        try
                        {
                            var team = teamLookup.Get(league, bid.TeamId);

                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { team.Id },
                                types: new WatcherType[] { WatcherType.Bids }
                            )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                            if (watchers.Count() > 0)
                            {
                                var url = api.GetBidUrl(league, bid);

                                var message = league.HasPluralTeamNames()
                                    ? $"The **{MessageUtils.Escape(team.Name)}** have won bidding on **{MessageUtils.Escape(bid.PlayerName)}** with a bid of ${bid.Amount.ToString("N0")}"
                                    : $"**{MessageUtils.Escape(team.Name)}** has won bidding on **{MessageUtils.Escape(bid.PlayerName)}** with a bid of ${bid.Amount.ToString("N0")}";

                                await _guildMessageService.SaveAsync(watchers.Select(watcher =>
                                    new GuildMessage
                                    {
                                        GuildId = watcher.Key.GuildId,
                                        ChannelId = watcher.Key.ChannelId,
                                        Color = Color.Orange,
                                        Title = $"{league.ShortName} Winning Bid",
                                        Thumbnail = league.LogoUrl,
                                        Content = message,
                                        Url = url,
                                    }));
                            }
                        }
                        catch (KeyNotFoundException e)
                        {
                            _logger.LogWarning(e, $"Failed to map teams for bid {bid.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"An unexpected error has occurred while processing bid {bid.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }

                        league.State.LastBid = bid.GetHash();
                    }

                    if (data.Count() > 0)
                        _logger.LogTrace($"Successfully processed {MessageUtils.Pluralize(data.Count(), "new winning bid")} for league \"{league.Name}\" [{league.Id}]");
                }
                else if (league.State.LastBid == null)
                    league.State.LastBid = data?.LastOrDefault()?.GetHash() ?? "";

                await _leagueService.SaveStateAsync(league, LeagueStateType.Bid);
            }));

            sw.Stop();
            _logger.LogTrace($"Bid tracking task completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Bid tracking task failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during bid tracking task.");
        }
    }
}