using System.Text.Json;
using Duthie.Types.Games;
using Duthie.Types.Leagues;

namespace Duthie.Modules.TheSpnhl.Tests;

public class TheSpnhlApiTests
{
    private readonly IReadOnlyCollection<string> EXCLUDED_TEAMS = new string[] { "Ducks", "Coyotes", "Bruins", "Sabres", "Hurricanes", "Stars", "Red Wings", "Oilers", "Panthers", "Kings", "Predators", "Devils", "Islanders", "Senators", "Sharks", "Blues", "Maple Leafs", "Canucks", "Golden Knights", "Capitals" };

    private readonly TheSpnhlApi _api;
    private readonly League _league;

    public TheSpnhlApiTests()
    {
        _api = new TheSpnhlApi();
        _league = new TheSpnhlLeagueProvider().Leagues.First(l => l.Id == new Guid("6991c990-a4fa-488b-884a-79b00e4e3577"));
        (_league.Info as TheSpnhlLeagueInfo)!.SeasonId = 43;
    }

    [Fact]
    public void Supports_TheSpnhl()
    {
        Assert.True(_api.Supports.Contains(TheSpnhlSiteProvider.SITE_ID), $"{_api.GetType().Name} does not support site {TheSpnhlSiteProvider.SITE_ID}");
    }

    [Fact]
    public async Task GetLeagueInfoAsync_ReturnsExpectedLeagueInfo()
    {
        var league = await _api.GetLeagueInfoAsync(_league);
        Assert.True(league != null, $"{_api.GetType().Name} does not support league {_league.Id}");
        Assert.True(_league.Name == league!.Name, $"expected Name to be {_league.Name} but got {league.Name}");
        Assert.True(league.Info is TheSpnhlLeagueInfo, $"expected Info to be of type {typeof(TheSpnhlLeagueInfo).Name} but got {league.Info?.GetType().Name ?? "null"}");

        var expectedInfo = JsonSerializer.Deserialize<TheSpnhlLeagueInfo>(File.ReadAllText(@"./Files/info.json"))!;
        var actualInfo = (league.Info as TheSpnhlLeagueInfo)!;
        Assert.True(expectedInfo.LeagueType == actualInfo.LeagueType, $"expected Info.LeagueType to be {expectedInfo.LeagueType} but got {actualInfo.LeagueType}");
        Assert.True(expectedInfo.SeasonId <= actualInfo.SeasonId, $"expected Info.SeasonId to be greater than or equal to {expectedInfo.SeasonId} but got {actualInfo.SeasonId}");
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsExpectedTeams()
    {
        var expectedTeams = JsonSerializer.Deserialize<IEnumerable<LeagueTeam>>(File.ReadAllText(@"./Files/teams.json"))!;
        var actualTeams = await _api.GetTeamsAsync(_league);
        Assert.True(actualTeams != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        var expectedTeamCount = expectedTeams.Count();
        var actualTeamCount = actualTeams!.Count();
        Assert.True(expectedTeamCount == actualTeamCount, $"expected {expectedTeamCount} teams but found {actualTeamCount}");

        foreach (var expectedTeam in expectedTeams)
        {
            var actualTeam = actualTeams!.FirstOrDefault(t => t.IId == expectedTeam.IId);
            Assert.True(actualTeam != null, $"team with IId {expectedTeam.IId} not found");
            Assert.True(expectedTeam.LeagueId == actualTeam!.LeagueId, $"expected LeagueId {expectedTeam.LeagueId} but got {actualTeam.LeagueId}");
            Assert.True(expectedTeam.TeamId == actualTeam!.TeamId, $"expected TeamId {expectedTeam.TeamId} but got {actualTeam.TeamId}");
            Assert.True(expectedTeam.IId == actualTeam.IId, $"expected IId {expectedTeam.IId} but got {actualTeam.IId}");
            Assert.True(expectedTeam.Team.Name == actualTeam.Team.Name, $"expected Name {expectedTeam.Team.Name} but got {actualTeam.Team.Name}");
            Assert.True(expectedTeam.Team.ShortName == actualTeam.Team.ShortName, $"expected ShortName {expectedTeam.Team.ShortName} but got {actualTeam.Team.ShortName}");
        }
    }

    [Fact]
    public async Task GetGamesAsync_ReturnsExpectedGames()
    {
        var league = await _api.GetLeagueInfoAsync(_league);

        var expectedGames = JsonSerializer.Deserialize<IEnumerable<ApiGame>>(File.ReadAllText(@"./Files/games.json"))!;
        var actualGames = await _api.GetGamesAsync(_league);
        Assert.True(actualGames != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        // thespnhl.com doesn't provide historical schedules. Once the season is updated, the rest of the tests
        // will fail.
        if ((league!.Info as TheSpnhlLeagueInfo)!.SeasonId != (_league.Info as TheSpnhlLeagueInfo)!.SeasonId)
            return;

        var expectedTeamCount = expectedGames.Count();
        var actualGameCount = actualGames!.Count();
        Assert.True(expectedTeamCount == actualGameCount, $"expected {expectedTeamCount} games but found {actualGameCount}");

        foreach (var expectedGame in expectedGames)
        {
            var actualGame = actualGames!.FirstOrDefault(g => g.GameId == expectedGame.GameId);
            Assert.True(actualGame != null, $"game with GameId {expectedGame.GameId} not found");
            Assert.True(expectedGame.LeagueId == actualGame!.LeagueId, $"expected LeagueId {expectedGame.LeagueId} but got {actualGame.LeagueId}");
            Assert.True(expectedGame.GameId == actualGame.GameId, $"expected GameId {expectedGame.GameId} but got {actualGame.GameId}");
            Assert.True(expectedGame.Date == actualGame.Date, $"expected Date {expectedGame.Date} but got {actualGame.Date}");
            Assert.True(expectedGame.VisitorIId == actualGame.VisitorIId, $"expected VisitorIId {expectedGame.VisitorIId} but got {actualGame.VisitorIId}");
            Assert.True(expectedGame.VisitorScore == actualGame.VisitorScore, $"expected VisitorScore {expectedGame.VisitorScore} but got {actualGame.VisitorScore}");
            Assert.True(expectedGame.HomeIId == actualGame.HomeIId, $"expected LeagueId {expectedGame.HomeIId} but got {actualGame.HomeIId}");
            Assert.True(expectedGame.HomeScore == actualGame.HomeScore, $"expected LeagueId {expectedGame.HomeScore} but got {actualGame.HomeScore}");
            Assert.True(expectedGame.Overtime == actualGame.Overtime, $"expected LeagueId {expectedGame.LeagueId} but got {actualGame.Overtime}");
            Assert.True(expectedGame.Shootout == actualGame.Shootout, $"expected LeagueId {expectedGame.Shootout} but got {actualGame.Shootout}");
        }
    }
}