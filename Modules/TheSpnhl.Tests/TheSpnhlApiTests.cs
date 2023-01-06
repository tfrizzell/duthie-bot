using System.Text.Json;
using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Modules.TheSpnhl.Tests;

public class TheSpnhlApiTests
{
    private readonly IReadOnlyCollection<string> EXCLUDED_TEAMS = new string[] { "Ducks", "Coyotes", "Bruins", "Sabres", "Hurricanes", "Stars", "Red Wings", "Oilers", "Panthers", "Kings", "Predators", "Devils", "Islanders", "Senators", "Sharks", "Blues", "Maple Leafs", "Canucks", "Golden Knights", "Capitals" };

    private readonly TheSpnhlApi _api;
    private readonly League _league;

    public TheSpnhlApiTests()
    {
        _api = new TheSpnhlApi();
        _league = new TheSpnhlLeagueProvider().Leagues.First(l => l.Id == TheSpnhlLeagueProvider.SPNHL.Id);
        (_league.Info as TheSpnhlLeagueInfo)!.SeasonId = 43;
    }

    [Fact]
    public void Supports_TheSpnhl()
    {
        Assert.True(_api.Supports.Contains(TheSpnhlSiteProvider.SPNHL.Id), $"{_api.GetType().Name} does not support site {TheSpnhlSiteProvider.SPNHL.Id}");
    }

    [Fact]
    public async Task GetLeagueInfoAsync_ReturnsExpectedLeagueInfo()
    {
        var league = await _api.GetLeagueAsync(_league);
        Assert.True(league != null, $"{_api.GetType().Name} does not support league {_league.Id}");
        Assert.True(_league.Name == league!.Name, $"expected Name to be {_league.Name} but got {league.Name}");
        Assert.True(_league.LogoUrl == league.LogoUrl, $"expected LogoUrl to be {_league.LogoUrl} but got {league.LogoUrl}");
        Assert.True(league.Info is TheSpnhlLeagueInfo, $"expected Info to be of type {typeof(TheSpnhlLeagueInfo).Name} but got {league.Info?.GetType().Name ?? "null"}");

        var expectedInfo = JsonSerializer.Deserialize<TheSpnhlLeagueInfo>(File.ReadAllText(@"./Files/info.json"))!;
        var actualInfo = (league.Info as TheSpnhlLeagueInfo)!;
        Assert.True(expectedInfo.LeagueType == actualInfo.LeagueType, $"expected Info.LeagueType to be {expectedInfo.LeagueType} but got {actualInfo.LeagueType}");
        Assert.True(expectedInfo.SeasonId <= actualInfo.SeasonId, $"expected Info.SeasonId to be greater than or equal to {expectedInfo.SeasonId} but got {actualInfo.SeasonId}");
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsExpectedTeams()
    {
        var league = await _api.GetLeagueAsync(_league);

        var actualTeams = await _api.GetTeamsAsync(_league);
        Assert.True(actualTeams != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        // thespnhl.com doesn't provide historical schedules. Once the season is updated, the rest of the tests
        // will fail.
        if ((league!.Info as TheSpnhlLeagueInfo)!.SeasonId != (_league.Info as TheSpnhlLeagueInfo)!.SeasonId)
            return;

        var expectedTeams = JsonSerializer.Deserialize<IEnumerable<Team>>(File.ReadAllText(@"./Files/teams.json"))!;
        var expectedTeamCount = expectedTeams.Count();
        var actualTeamCount = actualTeams!.Count();
        Assert.True(expectedTeamCount == actualTeamCount, $"expected {expectedTeamCount} teams but found {actualTeamCount}");

        foreach (var expectedTeam in expectedTeams)
        {
            var actualTeam = actualTeams!.FirstOrDefault(t => t.Id == expectedTeam.Id && t.Name == expectedTeam.Name);
            Assert.True(actualTeam != null, $"[team {expectedTeam.Id}] team not found");
            Assert.True(expectedTeam.LeagueId == actualTeam!.LeagueId, $"[team {expectedTeam.Id}] expected LeagueId {expectedTeam.LeagueId} but got {actualTeam.LeagueId}");
            Assert.True(expectedTeam.Id == actualTeam.Id, $"[team {expectedTeam.Id}] expected Id {expectedTeam.Id} but got {actualTeam.Id}");
            Assert.True(expectedTeam.Name == actualTeam.Name, $"[team {expectedTeam.Id}] expected Name {expectedTeam.Name} but got {actualTeam.Name}");
            Assert.True(expectedTeam.ShortName == actualTeam.ShortName, $"[team {expectedTeam.Id}] expected ShortName {expectedTeam.ShortName} but got {actualTeam.ShortName}");
        }
    }

    [Fact]
    public async Task GetGamesAsync_ReturnsExpectedGames()
    {
        var league = await _api.GetLeagueAsync(_league);

        var actualGames = await _api.GetGamesAsync(_league);
        Assert.True(actualGames != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        // thespnhl.com doesn't provide historical schedules. Once the season is updated, the rest of the tests
        // will fail.
        if ((league!.Info as TheSpnhlLeagueInfo)!.SeasonId != (_league.Info as TheSpnhlLeagueInfo)!.SeasonId)
            return;

        var expectedGames = JsonSerializer.Deserialize<IEnumerable<Game>>(File.ReadAllText(@"./Files/games.json"))!;
        var expectedTeamCount = expectedGames.Count();
        var actualGameCount = actualGames!.Count();
        Assert.True(expectedTeamCount == actualGameCount, $"expected {expectedTeamCount} games but found {actualGameCount}");

        foreach (var expectedGame in expectedGames)
        {
            var actualGame = actualGames!.FirstOrDefault(g => g.Id == expectedGame.Id);
            Assert.True(actualGame != null, $"[game {expectedGame.Id}] game not found");
            Assert.True(expectedGame.LeagueId == actualGame!.LeagueId, $"[game {expectedGame.Id}] expected LeagueId to be {expectedGame.LeagueId} but got {actualGame.LeagueId}");
            Assert.True(expectedGame.Id == actualGame.Id, $"[game {expectedGame.Id}] expected GameId to be {expectedGame.Id} but got {actualGame.Id}");
            Assert.True(expectedGame.Timestamp == actualGame.Timestamp, $"[game {expectedGame.Id}] expected Timestamp to be {expectedGame.Timestamp} but got {actualGame.Timestamp}");
            Assert.True(expectedGame.VisitorId == actualGame.VisitorId, $"[game {expectedGame.Id}] expected VisitorId to be {expectedGame.VisitorId} but got {actualGame.VisitorId}");
            Assert.True(expectedGame.VisitorScore == actualGame.VisitorScore, $"[game {expectedGame.Id}] expected VisitorScore to be {expectedGame.VisitorScore} but got {actualGame.VisitorScore}");
            Assert.True(expectedGame.HomeId == actualGame.HomeId, $"[game {expectedGame.Id}] expected HomeId to be {expectedGame.HomeId} but got {actualGame.HomeId}");
            Assert.True(expectedGame.HomeScore == actualGame.HomeScore, $"[game {expectedGame.Id}] expected HomeScore to be {expectedGame.HomeScore} but got {actualGame.HomeScore}");
            Assert.True(expectedGame.Overtime == actualGame.Overtime, $"[game {expectedGame.Id}] expected Overtime to be {expectedGame.Overtime} but got {actualGame.Overtime}");
            Assert.True(expectedGame.Shootout == actualGame.Shootout, $"[game {expectedGame.Id}] expected Shootout to be {expectedGame.Shootout} but got {actualGame.Shootout}");
        }
    }
}