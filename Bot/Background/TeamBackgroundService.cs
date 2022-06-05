using System.Diagnostics;
using System.Text.RegularExpressions;
using Duthie.Services.Api;
using Duthie.Services.Leagues;
using Duthie.Services.Teams;
using Duthie.Types.Api;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public class TeamBackgroundService : ScheduledBackgroundService
{
    private readonly ILogger<TeamBackgroundService> _logger;
    private readonly ApiService _apiService;
    private readonly LeagueService _leagueService;
    private readonly TeamService _teamService;

    public TeamBackgroundService(
        ILogger<TeamBackgroundService> logger,
        ApiService apiService,
        LeagueService leagueService,
        TeamService teamService) : base(logger)
    {
        _logger = logger;
        _apiService = apiService;
        _leagueService = leagueService;
        _teamService = teamService;
    }

    protected override string[] Schedules
    {
        get => new string[]
        {
            "30 */12 * * *"
        };
    }

    public override async Task ExecuteAsync(CancellationToken? cancellationToken = null)
    {
        _logger.LogTrace("Starting team update task");
        var sw = Stopwatch.StartNew();

        try
        {
            var leagues = await _leagueService.GetAllAsync();

            var teams = (await _teamService.GetAllAsync())
                .ToDictionary(t => CreateKey(t.Name), StringComparer.OrdinalIgnoreCase);

            await Task.WhenAll(leagues.Select(async league =>
            {
                var api = _apiService.Get<ITeamApi>(league);

                if (api == null)
                    return;

                var data = await api.GetTeamsAsync(league);

                if (data == null)
                    return;

                foreach (var lt in data)
                {
                    var key = CreateKey(lt.Team.Name);

                    if (!teams.ContainsKey(key))
                    {
                        teams.Add(key, lt.Team);
                        continue;
                    }

                    lt.TeamId = teams[key]?.Id ?? lt.TeamId;
                    lt.Team = teams[key] ?? lt.Team;
                }

                league.LeagueTeams = data;
            }));

            await _teamService.SaveAsync(teams.Values);
            await _leagueService.SaveAsync(leagues);

            sw.Stop();
            _logger.LogTrace($"Team update task completed in {sw.Elapsed.TotalMilliseconds}ms");
        }
        catch (Exception e)
        {
            sw.Stop();
            _logger.LogTrace($"Team update task failed in {sw.Elapsed.TotalMilliseconds}ms");
            _logger.LogError(e, "An unexpected error during team update task.");
        }
    }

    private static string CreateKey(string name) =>
        Regex.Replace(name, @"[^0-9a-zA-Z]", "").ToUpper();
}