using System.Diagnostics;
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
                    .OrderBy(b => b.Timestamp)
                    .ToList();

                if (data?.Count() > 0 && league.State.LastContract != null)
                {
                    var LastContractIndex = data.FindIndex(b => b.GetHash() == league.State.LastContract);

                    if (LastContractIndex >= 0)
                        data.RemoveRange(0, LastContractIndex + 1);

                    foreach (var contract in data)
                    {
                        try
                        {
                            var team = FindTeam(league, contract.TeamExternalId);
                            var messages = new List<GuildMessage>();

                            var (message, embed) = api.GetMessageEmbed(
                                league.Tags.Intersect(new string[] { "esports", "tournament", "pickup", "club teams" }).Count() > 0
                                    ? $"**{MessageUtils.Escape(team.Name)}** has signed **{MessageUtils.Escape(contract.PlayerName)}** to a {contract.Length}-season contract worth ${contract.Amount.ToString("N0")} per season!"
                                    : $"The **{MessageUtils.Escape(team.Name)}** have signed **{MessageUtils.Escape(contract.PlayerName)}** to a {contract.Length}-season contract worth ${contract.Amount.ToString("N0")} per season!",
                                contract, league);

                            var watchers = (await _watcherService.FindAsync(
                                leagues: new Guid[] { league.Id },
                                teams: new Guid[] { team.Id },
                                types: new WatcherType[] { WatcherType.Contracts }
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
                            _logger.LogError(e, $"Failed to map teams for contract {contract.GetHash()} in league {league.Id}");
                        }

                        league.State.LastContract = contract.GetHash();
                    }
                }
                else
                    league.State.LastContract = data?.LastOrDefault()?.GetHash() ?? "";

                await _leagueService.SaveAsync(league);
            }));

            sw.Stop();
            _logger.LogTrace($"Contract tracking task completed in {sw.Elapsed.TotalMilliseconds}ms");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Contract tracking task failed in {sw.Elapsed.TotalMilliseconds}ms");
            _logger.LogError(e, "An unexpected error during contract tracking task.");
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