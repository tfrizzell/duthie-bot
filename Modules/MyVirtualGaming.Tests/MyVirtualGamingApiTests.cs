using System.Text.Json;
using Duthie.Types.Api;
using Duthie.Types.Leagues;
using Duthie.Types.Teams;

namespace Duthie.Modules.MyVirtualGaming.Tests;

public class MyVirtualGamingApiTests
{
    private readonly MyVirtualGamingApi _api;
    private readonly League _league;

    public MyVirtualGamingApiTests()
    {
        _api = new MyVirtualGamingApi();
        _league = new MyVirtualGamingLeagueProvider().Leagues.First(l => l.Id == new Guid("5957b164-7bb5-4324-967a-16c3044260b2"));
        (_league.Info as MyVirtualGamingLeagueInfo)!.SeasonId = 72;
    }

    [Fact]
    public void Supports_MyVirtualGaming()
    {
        Assert.True(_api.Supports.Contains(MyVirtualGamingSiteProvider.SITE_ID), $"{_api.GetType().Name} does not support site {MyVirtualGamingSiteProvider.SITE_ID}");
    }

    [Fact]
    public async Task GetLeagueInfoAsync_ReturnsExpectedLeagueInfo()
    {
        var league = await _api.GetLeagueInfoAsync(_league);
        Assert.True(league != null, $"{_api.GetType().Name} does not support league {_league.Id}");
        Assert.True(_league.Name == league!.Name, $"expected Name to be {_league.Name} but got {league.Name}");
        Assert.True(league.Info is MyVirtualGamingLeagueInfo, $"expected Info to be of type {typeof(MyVirtualGamingLeagueInfo).Name} but got {league.Info?.GetType().Name ?? "null"}");

        var expectedInfo = JsonSerializer.Deserialize<MyVirtualGamingLeagueInfo>(File.ReadAllText(@"./Files/info.json"))!;
        var actualInfo = (league.Info as MyVirtualGamingLeagueInfo)!;
        Assert.True(expectedInfo.LeagueId == actualInfo.LeagueId, $"expected Info.LeagueId to be {expectedInfo.LeagueId} but got {actualInfo.LeagueId}");
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
            var actualTeam = actualTeams!.FirstOrDefault(t => t.ExternalId == expectedTeam.ExternalId && t.Team.Name == expectedTeam.Team.Name);
            Assert.True(actualTeam != null, $"[team {expectedTeam.ExternalId}] team not found");
            Assert.True(expectedTeam.LeagueId == actualTeam!.LeagueId, $"[team {expectedTeam.ExternalId}] expected LeagueId {expectedTeam.LeagueId} but got {actualTeam.LeagueId}");
            Assert.True(expectedTeam.TeamId == actualTeam!.TeamId, $"[team {expectedTeam.ExternalId}] expected TeamId {expectedTeam.TeamId} but got {actualTeam.TeamId}");
            Assert.True(expectedTeam.ExternalId == actualTeam.ExternalId, $"[team {expectedTeam.ExternalId}] expected ExternalId {expectedTeam.ExternalId} but got {actualTeam.ExternalId}");
            Assert.True(expectedTeam.Team.Name == actualTeam.Team.Name, $"[team {expectedTeam.ExternalId}] expected Name {expectedTeam.Team.Name} but got {actualTeam.Team.Name}");
            Assert.True(expectedTeam.Team.ShortName == actualTeam.Team.ShortName, $"[team {expectedTeam.ExternalId}] expected ShortName {expectedTeam.Team.ShortName} but got {actualTeam.Team.ShortName}");
        }
    }

    [Fact]
    public async Task GetGamesAsync_ReturnsExpectedGames()
    {
        var expectedGames = JsonSerializer.Deserialize<IEnumerable<Game>>(File.ReadAllText(@"./Files/games.json"))!;
        var actualGames = await _api.GetGamesAsync(_league);
        Assert.True(actualGames != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        var expectedTeamCount = expectedGames.Count();
        var actualGameCount = actualGames!.Count();
        Assert.True(expectedTeamCount == actualGameCount, $"expected {expectedTeamCount} games but found {actualGameCount}");

        foreach (var expectedGame in expectedGames)
        {
            var actualGame = actualGames!.FirstOrDefault(g => g.GameId == expectedGame.GameId);
            Assert.True(actualGame != null, $"[game {expectedGame.GameId}] game not found");
            Assert.True(expectedGame.LeagueId == actualGame!.LeagueId, $"[game {expectedGame.GameId}] expected LeagueId to be {expectedGame.LeagueId} but got {actualGame.LeagueId}");
            Assert.True(expectedGame.GameId == actualGame.GameId, $"[game {expectedGame.GameId}] expected GameId to be {expectedGame.GameId} but got {actualGame.GameId}");
            Assert.True(expectedGame.Timestamp == actualGame.Timestamp, $"[game {expectedGame.GameId}] expected Timestamp to be {expectedGame.Timestamp} but got {actualGame.Timestamp}");
            Assert.True(expectedGame.VisitorExternalId == actualGame.VisitorExternalId, $"[game {expectedGame.GameId}] expected VisitorExternalId to be {expectedGame.VisitorExternalId} but got {actualGame.VisitorExternalId}");
            Assert.True(expectedGame.VisitorScore == actualGame.VisitorScore, $"[game {expectedGame.GameId}] expected VisitorScore to be {expectedGame.VisitorScore} but got {actualGame.VisitorScore}");
            Assert.True(expectedGame.HomeExternalId == actualGame.HomeExternalId, $"[game {expectedGame.GameId}] expected HomeExternalId to be {expectedGame.HomeExternalId} but got {actualGame.HomeExternalId}");
            Assert.True(expectedGame.HomeScore == actualGame.HomeScore, $"[game {expectedGame.GameId}] expected HomeScore to be {expectedGame.HomeScore} but got {actualGame.HomeScore}");
            Assert.True(expectedGame.Overtime == actualGame.Overtime, $"[game {expectedGame.GameId}] expected Overtime to be {expectedGame.Overtime} but got {actualGame.Overtime}");
            Assert.True(expectedGame.Shootout == actualGame.Shootout, $"[game {expectedGame.GameId}] expected Shootout to be {expectedGame.Shootout} but got {actualGame.Shootout}");
        }
    }

    [Fact]
    public async Task GetBidsAsync_ReturnsNotNull()
    {
        var bids = await _api.GetBidsAsync(_league);
        Assert.True(bids != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        foreach (var bid in bids!)
        {
            Assert.True(_league.Id == bid.LeagueId, $"expected LeagueId to be {_league.Id} but got {bid.LeagueId}");
            Assert.True(BidState.Won == bid.State, $"expected State to be {BidState.Won} but got {bid.State}");
        }
    }
}