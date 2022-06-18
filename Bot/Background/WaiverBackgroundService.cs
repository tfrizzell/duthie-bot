using System.Diagnostics;
using Duthie.Bot.Utils;
using Duthie.Services.Api;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Watchers;
using Duthie.Types.Modules.Api;
using Duthie.Types.Modules.Data;
using Duthie.Types.Guilds;
using Duthie.Types.Leagues;
using Duthie.Types.Watchers;
using Microsoft.Extensions.Logging;
using Duthie.Bot.Extensions;

namespace Duthie.Bot.Background;

public class WaiverBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<WaiverBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly WatcherService _watcherService;
    private readonly GuildMessageService _guildMessageService;

    public WaiverBackgroundService(
        ILogger<WaiverBackgroundService> logger,
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
        _logger.LogTrace("Starting waiver tracking task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();
            var teamLookup = new TeamLookup(leagues);

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<IWaiverApi>(league);

                if (api == null)
                    return;

                var data = (await api.GetWaiversAsync(league))?
                    .OrderBy(w => w.Timestamp)
                        .ThenBy(w => w.GetHash())
                    .ToList();

                if (data?.Count() > 0 && league.State.LastWaiver != null)
                {
                    var LastWaiverIndex = data.FindIndex(w => w.GetHash() == league.State.LastWaiver);

                    if (LastWaiverIndex >= 0)
                        data.RemoveRange(0, LastWaiverIndex + 1);

                    foreach (var waiver in data)
                    {
                        try
                        {
                            var team = teamLookup.Get(league, waiver.TeamId);

                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { team.Id },
                                types: new WatcherType[] { WatcherType.Waivers }
                            )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                            if (watchers.Count() > 0)
                            {
                                var timestamp = DateTimeOffset.UtcNow;
                                var url = api.GetWaiverUrl(league, waiver);

                                var message = waiver.Type switch
                                {
                                    WaiverActionType.Placed => league.HasPluralTeamNames()
                                        ? $"The **{MessageUtils.Escape(team.Name)}** have placed **{MessageUtils.Escape(waiver.PlayerName)}** on waivers"
                                        : $"**{MessageUtils.Escape(team.Name)}** has placed **{MessageUtils.Escape(waiver.PlayerName)}** on waivers",
                                    WaiverActionType.Removed => league.HasPluralTeamNames()
                                        ? $"The **{MessageUtils.Escape(team.Name)}** have removed **{MessageUtils.Escape(waiver.PlayerName)}** off of waivers"
                                        : $"**{MessageUtils.Escape(team.Name)}** has removed **{MessageUtils.Escape(waiver.PlayerName)}** off of waivers",
                                    WaiverActionType.Claimed => league.HasPluralTeamNames()
                                        ? $"The **{MessageUtils.Escape(team.Name)}** have claimed **{MessageUtils.Escape(waiver.PlayerName)}** off of waivers"
                                        : $"**{MessageUtils.Escape(team.Name)}** has claimed **{MessageUtils.Escape(waiver.PlayerName)}** off of waivers",
                                    WaiverActionType.Cleared => league.HasPluralTeamNames()
                                        ? $"**{MessageUtils.Escape(waiver.PlayerName)}** has cleared waivers and is reporting to the **{MessageUtils.Escape(team.Name)}**"
                                        : $"**{MessageUtils.Escape(waiver.PlayerName)}** has cleared waivers and is reporting to **{MessageUtils.Escape(team.Name)}**",
                                    _ => "",
                                };

                                if (!string.IsNullOrWhiteSpace(message))
                                {
                                    await _guildMessageService.SaveAsync(watchers.Select(watcher =>
                                        new GuildMessage
                                        {
                                            GuildId = watcher.Key.GuildId,
                                            ChannelId = watcher.Key.ChannelId,
                                            Color = Colors.Chocolate,
                                            Title = $"{league.ShortName} Waiver Wire",
                                            Thumbnail = league.LogoUrl,
                                            Content = message,
                                            Url = url,
                                            Timestamp = timestamp,
                                        }));
                                }
                            }
                        }
                        catch (KeyNotFoundException e)
                        {
                            if (!teamLookup.Has(league.Site, waiver.TeamId))
                                _logger.LogWarning(e, $"Failed to map teams for waiver {waiver.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"An unexpected error has occurred while processing waiver {waiver.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }

                        league.State.LastWaiver = waiver.GetHash();
                    }

                    if (data.Count() > 0)
                        _logger.LogTrace($"Successfully processed {MessageUtils.Pluralize(data.Count(), "new waiver")} for league \"{league.Name}\" [{league.Id}]");
                }
                else
                    league.State.LastWaiver = data?.LastOrDefault()?.GetHash() ?? "";

                await _leagueService.SaveStateAsync(league, LeagueStateType.Waiver);
            }));

            sw.Stop();
            _logger.LogTrace($"Waiver tracking task completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Waiver tracking task failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during waiver tracking task.");
        }
    }
}