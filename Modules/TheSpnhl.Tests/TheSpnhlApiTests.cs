using Duthie.Types;

namespace Duthie.Modules.TheSpnhl.Tests;

public class TheSpnhlApiTests
{
    private readonly IReadOnlyCollection<string> EXCLUDED_TEAMS = new string[] { "Ducks", "Coyotes", "Bruins", "Sabres", "Hurricanes", "Stars", "Red Wings", "Oilers", "Panthers", "Kings", "Predators", "Devils", "Islanders", "Senators", "Sharks", "Blues", "Maple Leafs", "Canucks", "Golden Knights", "Capitals" };

    private static readonly Guid LEAGUE_ID = new Guid("6991c990-a4fa-488b-884a-79b00e4e3577");
    private const int EXPECTED_SEASON_ID = 43;

    private readonly TheSpnhlApi _api;
    private readonly League _league;

    public TheSpnhlApiTests()
    {
        _api = new TheSpnhlApi();
        _league = new TheSpnhlLeagueProvider().Leagues.First(l => l.Id == LEAGUE_ID);
    }

    [Fact]
    public void Supports_TheSpnhl()
    {
        Assert.True(_api.Supports.Contains(TheSpnhlSiteProvider.SITE_ID), $"{_api.GetType().Name} does not support site {TheSpnhlSiteProvider.SITE_ID}");
    }

    [Fact]
    public async Task GetLeagueInfoAsync_ReturnsLeagueInfo()
    {
        var league = await _api.GetLeagueInfoAsync(_league);
        Assert.True(league != null, $"{_api.GetType().Name} does not support league {_league.Id}");
        Assert.True(_league.Name.Equals(league!.Name), $"expected Name to be {_league.Name} but got {league.Name}");
        Assert.True(league?.Info is TheSpnhlLeagueInfo, $"expected Info to be of type {typeof(TheSpnhlLeagueInfo).Name} but got {league?.Info?.GetType()?.Name ?? "null"}");

        var info = (league?.Info as TheSpnhlLeagueInfo)!;
        Assert.True(EXPECTED_SEASON_ID <= info.SeasonId, $"expected Info.SeasonId to be greater than or equal to {EXPECTED_SEASON_ID} but got {info.SeasonId}");
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsNHLTeams()
    {
        var teams = await _api.GetTeamsAsync(_league);
        Assert.True(teams != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        var expectedCount = DefaultTeams.NHL.Count(t => !EXCLUDED_TEAMS.Contains(t.Name) && !EXCLUDED_TEAMS.Contains(t.ShortName));
        var actualCount = teams!.Count();
        Assert.True(expectedCount == actualCount, $"expected {expectedCount} teams but found {actualCount}");

        var badLeagueCount = teams!.Count(t => t.LeagueId != _league.Id);
        Assert.True(badLeagueCount == 0, $"expected all teams to have LeagueId {_league.Id} but found {badLeagueCount} that did not");

        foreach (var team in DefaultTeams.NHL)
        {
            if (EXCLUDED_TEAMS.Contains(team.Name) || EXCLUDED_TEAMS.Contains(team.ShortName))
                continue;

            var leagueTeam = teams?.FirstOrDefault(t => t.Team.Name.Equals(team.Name) && t.Team.ShortName.Equals(team.ShortName));
            Assert.True(team.Name.Equals(leagueTeam?.Team.Name), $"expected Name {team.Name} but got {leagueTeam?.Team.Name}");
            Assert.True(team.ShortName.Equals(leagueTeam?.Team.ShortName), $"expected ShortName {team.ShortName} but got {leagueTeam?.Team.ShortName}");
            Assert.True(!string.IsNullOrWhiteSpace(leagueTeam?.IId), $"expected IId to non-empty but got empty");
        }
    }
}