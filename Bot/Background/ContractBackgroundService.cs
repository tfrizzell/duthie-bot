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

namespace Duthie.Bot.Background;

public class ContractBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<ContractBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly WatcherService _watcherService;
    private readonly GuildMessageService _guildMessageService;

    public ContractBackgroundService(
        ILogger<ContractBackgroundService> logger,
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
        _logger.LogTrace("Starting contract tracking task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<IContractApi>(league);

                if (api == null)
                    return;

                var data = (await api.GetContractsAsync(league))?
                    .OrderBy(c => c.Timestamp)
                        .ThenBy(c => c.GetHash())
                    .ToList();

                if (data?.Count() > 0 && league.State.LastContract != null)
                {
                    var LastContractIndex = data.FindIndex(c => c.GetHash() == league.State.LastContract);

                    if (LastContractIndex >= 0)
                        data.RemoveRange(0, LastContractIndex + 1);

                    foreach (var contract in data)
                    {
                        try
                        {
                            var team = FindTeam(league, contract.TeamId);

                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { team.Id },
                                types: new WatcherType[] { WatcherType.Contracts }
                            )).GroupBy(w => new { w.GuildId, ChannelId = w.ChannelId ?? w.Guild.DefaultChannelId });

                            if (watchers.Count() > 0)
                            {
                                var timestamp = DateTimeOffset.UtcNow;
                                var url = api.GetContractUrl(league, contract);

                                var message = league.HasPluralTeamNames()
                                    ? $"The **{MessageUtils.Escape(team.Name)}** have signed **{MessageUtils.Escape(contract.PlayerName)}** to a {contract.Length}-season contract worth ${contract.Amount.ToString("N0")} per season!"
                                    : $"**{MessageUtils.Escape(team.Name)}** has signed **{MessageUtils.Escape(contract.PlayerName)}** to a {contract.Length}-season contract worth ${contract.Amount.ToString("N0")} per season!";

                                await _guildMessageService.SaveAsync(watchers.Select(watcher =>
                                    new GuildMessage
                                    {
                                        GuildId = watcher.Key.GuildId,
                                        ChannelId = watcher.Key.ChannelId,
                                        Message = "",
                                        Embed = new GuildMessageEmbed
                                        {
                                            Color = Color.Gold,
                                            Title = $"{league.ShortName} Contract Signing",
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
                            _logger.LogWarning(e, $"Failed to map teams for contract {contract.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"An unexpected error has occurred while processing contract {contract.GetHash()} for league \"{league.Name}\" [{league.Id}]");
                        }

                        league.State.LastContract = contract.GetHash();
                    }

                    if (data.Count() > 0)
                        _logger.LogTrace($"Successfully processed {data.Count()} new contracts for league \"{league.Name}\" [{league.Id}]");
                }
                else
                    league.State.LastContract = data?.LastOrDefault()?.GetHash() ?? "";

                await _leagueService.SaveStateAsync(league.Id, LeagueStateType.Contract, league.State.LastContract);
            }));

            sw.Stop();
            _logger.LogTrace($"Contract tracking task completed in {sw.Elapsed.TotalSeconds}s");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Contract tracking task failed in {sw.Elapsed.TotalSeconds}s");
            _logger.LogError(e, "An unexpected error during contract tracking task.");
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