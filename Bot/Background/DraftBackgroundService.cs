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

public class DraftBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<DraftBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly WatcherService _watcherService;
    private readonly GuildMessageService _guildMessageService;

    public DraftBackgroundService(
        ILogger<DraftBackgroundService> logger,
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
        _logger.LogTrace("Starting draft pick tracking task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();
            var teamLookup = new TeamLookup(leagues);

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<IDraftApi>(league);

                if (api == null)
                    return;

                var data = (await api.GetDraftPicksAsync(league))?
                    .OrderBy(p => p.OverallPick)
                    .ToList();

                if (data?.Count() > 0 && league.State.LastDraftPick != null)
                {
                    var lastDraftIndex = data.FindIndex(b => b.GetHash() == league.State.LastDraftPick);

                    if (lastDraftIndex >= 0)
                        data.RemoveRange(0, lastDraftIndex + 1);

                    foreach (var draftPick in data)
                    {
                        try
                        {
                            var team = teamLookup.Get(league, draftPick.TeamId);

                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { team.Id },
                                types: new WatcherType[] { WatcherType.Draft }
                            )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                            if (watchers.Count() > 0)
                            {
                                var url = api.GetDraftPickUrl(league, draftPick);

                                var message = league.HasPluralTeamNames()
                                    ? $"The **{MessageUtils.Escape(team.Name)}** have selected **{MessageUtils.Escape(draftPick.PlayerName)}** with the {draftPick.RoundPick.Ordinal()} pick in the {draftPick.RoundNumber.Ordinal()} round _({draftPick.OverallPick.Ordinal()} Overall)_"
                                    : $"**{MessageUtils.Escape(team.Name)}** has selected **{MessageUtils.Escape(draftPick.PlayerName)}** with the {draftPick.RoundPick.Ordinal()} pick in the {draftPick.RoundNumber.Ordinal()} round _({draftPick.OverallPick.Ordinal()} Overall)_";

                                await _guildMessageService.SaveAsync(watchers.Select(watcher =>
                                    new GuildMessage
                                    {
                                        GuildId = watcher.Key.GuildId,
                                        ChannelId = watcher.Key.ChannelId,
                                        Color = Color.Teal,
                                        Title = $"{league.ShortName} Draft Pick",
                                        Thumbnail = league.LogoUrl,
                                        Content = message,
                                        Url = url,
                                    }));
                            }
                        }
                        catch (KeyNotFoundException e)
                        {
                            _logger.LogWarning(e, $"Failed to map teams for draft pick {draftPick.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"An unexpected error has occurred while processing draft pick {draftPick.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }

                        league.State.LastDraftPick = draftPick.GetHash();
                    }

                    if (data.Count() > 0)
                        _logger.LogTrace($"Successfully processed {MessageUtils.Pluralize(data.Count(), "new draft pick")} for league \"{league.Name}\" [{league.Id}]");
                }
                else if (league.State.LastDraftPick == null)
                    league.State.LastDraftPick = data?.LastOrDefault()?.GetHash() ?? "";

                await _leagueService.SaveStateAsync(league, LeagueStateType.DraftPick);
            }));

            sw.Stop();
            _logger.LogTrace($"Draft pick tracking task completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Draft pick tracking task failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during draft pick tracking task.");
        }
    }
}