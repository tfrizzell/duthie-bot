using System.Diagnostics;
using System.Text.RegularExpressions;
using Discord;
using Duthie.Bot.Utils;
using Duthie.Bot.Extensions;
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

namespace Duthie.Bot.Background;

public class RosterTransactionBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<RosterTransactionBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly WatcherService _watcherService;
    private readonly GuildMessageService _guildMessageService;

    public RosterTransactionBackgroundService(
        ILogger<RosterTransactionBackgroundService> logger,
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
        _logger.LogTrace("Starting roster transaction tracking task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();
            var teamLookup = new TeamLookup(leagues);

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<IRosterApi>(league);

                if (api == null)
                    return;

                var data = (await api.GetRosterTransactionsAsync(league))?
                    .OrderBy(t => t.Timestamp)
                        .ThenBy(t => t.GetHash())
                    .ToList();

                if (data?.Count() > 0 && league.State.LastRosterTransaction != null)
                {
                    var LastRosterTransactionIndex = data.FindIndex(t => t.GetHash() == league.State.LastRosterTransaction);

                    if (LastRosterTransactionIndex >= 0)
                        data.RemoveRange(0, LastRosterTransactionIndex + 1);

                    foreach (var rosterTransaction in data)
                    {
                        try
                        {
                            var teams = rosterTransaction.TeamIds.Select(teamId => teamLookup.Get(league.Site, teamId));

                            if (teams.Count() > 0)
                            {
                                var leagueIds = new List<Guid> { league.Id };

                                if (rosterTransaction.TeamIds.Any(teamId => !teamLookup.Has(league, teamId)))
                                    leagueIds.AddRange(league.Affiliates.Select(a => a.AffiliateId));

                                var watchers = (await _watcherService.FindAsync(
                                    leagues: leagueIds,
                                    teams: teams.Select(t => t.Id),
                                    types: new WatcherType[] { WatcherType.Roster }
                                )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                                if (watchers.Count() > 0)
                                {
                                    var url = api.GetRosterTransactionUrl(league, rosterTransaction);

                                    var message = Regex.Replace(rosterTransaction.Type switch
                                    {
                                        RosterTransactionType.PlacedOnIr => league.HasPluralTeamNames()
                                            ? $"The **{MessageUtils.Escape(teams.First().Name)}** have placed **{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** on injured reserved"
                                            : $"**{MessageUtils.Escape(teams.First().Name)}** has placed **{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** on injured reserved",
                                        RosterTransactionType.RemovedFromIr => league.HasPluralTeamNames()
                                            ? $"The **{MessageUtils.Escape(teams.First().Name)}** have removed **{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** from injured reserved"
                                            : $"**{MessageUtils.Escape(teams.First().Name)}** has removed **{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** from injured reserved",
                                        RosterTransactionType.ReportedInactive => league.HasPluralTeamNames()
                                            ? $"The **{MessageUtils.Escape(teams.First().Name)}** have reported **{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** as inactive and removed them from their roster"
                                            : $"**{MessageUtils.Escape(teams.First().Name)}** has reported **{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** as inactive and removed them from their roster",
                                        RosterTransactionType.CalledUp => league.HasPluralTeamNames()
                                            ? $"The **{MessageUtils.Escape(teams.First().Name)}** have called **{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** up from the **{MessageUtils.Escape(teams.Last().Name)}**"
                                            : $"**{MessageUtils.Escape(teams.First().Name)}** has called **{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** up from **{MessageUtils.Escape(teams.Last().Name)}**",
                                        RosterTransactionType.SentDown => league.HasPluralTeamNames()
                                            ? $"The **{MessageUtils.Escape(teams.First().Name)}** have sent **{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** down to the **{MessageUtils.Escape(teams.Last().Name)}**"
                                            : $"**{MessageUtils.Escape(teams.First().Name)}** has sent **{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** down to **{MessageUtils.Escape(teams.Last().Name)}**",
                                        RosterTransactionType.Banned => league.HasPluralTeamNames()
                                            ? $"**{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** has been banned by the **{MessageUtils.Escape(teams.FirstOrDefault()?.Name ?? "")}**"
                                            : $"**{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** has been banned by **{MessageUtils.Escape(teams.FirstOrDefault()?.Name ?? "")}**",
                                        RosterTransactionType.Suspended => league.HasPluralTeamNames()
                                            ? $"**{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** has been suspended by the **{MessageUtils.Escape(teams.FirstOrDefault()?.Name ?? "")}**"
                                            : $"**{MessageUtils.Escape(rosterTransaction.PlayerNames.First())}** has been suspended by **{MessageUtils.Escape(teams.FirstOrDefault()?.Name ?? "")}**",
                                        _ => "",
                                    }, @" by\s*(the)?\s*\*\*\*\*", "").Trim();

                                    if (!string.IsNullOrWhiteSpace(message))
                                    {
                                        await _guildMessageService.SaveAsync(watchers.Select(watcher =>
                                            new GuildMessage
                                            {
                                                GuildId = watcher.Key.GuildId,
                                                ChannelId = watcher.Key.ChannelId,
                                                Color = Color.Purple,
                                                Title = $"{league.ShortName} Roster Transaction",
                                                Thumbnail = league.LogoUrl,
                                                Content = message,
                                                Url = url,
                                                Timestamp = rosterTransaction.Timestamp,
                                            }));
                                    }
                                }
                            }
                        }
                        catch (KeyNotFoundException e)
                        {
                            _logger.LogWarning(e, $"Failed to map teams for processing roster transaction {rosterTransaction.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"An unexpected error has occurred while processing roster transaction {rosterTransaction.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }

                        league.State.LastRosterTransaction = rosterTransaction.GetHash();
                    }

                    if (data.Count() > 0)
                        _logger.LogTrace($"Successfully processed {MessageUtils.Pluralize(data.Count(), "new roster transaction")} for league \"{league.Name}\" [{league.Id}]");
                }
                else
                    league.State.LastRosterTransaction = data?.LastOrDefault()?.GetHash() ?? "";

                await _leagueService.SaveStateAsync(league, LeagueStateType.RosterTransaction);
            }));

            sw.Stop();
            _logger.LogTrace($"Roster transaction tracking task completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Roster transaction tracking task failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during roster transaction tracking task.");
        }
    }
}