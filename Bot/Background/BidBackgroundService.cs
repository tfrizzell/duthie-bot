using System.Diagnostics;
using Duthie.Bot.Extensions;
using Duthie.Bot.Utils;
using Duthie.Services.Api;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Watchers;
using Duthie.Types.Api;
using Duthie.Types.Guilds;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;
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

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<IBidApi>(league);

                if (api == null)
                    return;

                var data = (await api.GetBidsAsync(league))?
                    .OrderBy(b => b.Timestamp)
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
                            var team = FindTeam(league, bid.TeamExternalId);
                            var messages = new List<GuildMessage>();

                            var (message, embed) = api.GetMessageEmbed(
                                league.HasPluralTeamNames()
                                    ? $"The **{MessageUtils.Escape(team.Name)}** have won bidding on **{MessageUtils.Escape(bid.PlayerName)}** with a bid of ${bid.Amount.ToString("N0")}!"
                                    : $"**{MessageUtils.Escape(team.Name)}** has won bidding on **{MessageUtils.Escape(bid.PlayerName)}** with a bid of ${bid.Amount.ToString("N0")}!",
                                bid, league);

                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { team.Id },
                                types: new WatcherType[] { WatcherType.Bids }
                            )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                            foreach (var watcher in watchers)
                            {
                                messages.Add(new GuildMessage
                                {
                                    GuildId = watcher.Key.GuildId,
                                    ChannelId = watcher.Key.ChannelId,
                                    Message = message,
                                    Embed = embed,
                                });
                            }

                            await _guildMessageService.SaveAsync(messages);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"Failed to map teams for bid {bid.GetHash()} in league {league.Id}");
                        }

                        league.State.LastBid = bid.GetHash();
                    }
                }
                else
                    league.State.LastBid = data?.LastOrDefault()?.GetHash() ?? "";

                await _leagueService.SaveAsync(league);
            }));

            sw.Stop();
            _logger.LogTrace($"Bid tracking task completed in {sw.Elapsed.TotalMilliseconds}ms");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Bid tracking task failed in {sw.Elapsed.TotalMilliseconds}ms");
            _logger.LogError(e, "An unexpected error during bid tracking task.");
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