using System.Text.Json;
using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Modules.MyVirtualGaming.Tests;

public class MyVirtualGamingApiTests
{
    private readonly MyVirtualGamingApi _api;
    private readonly League _league;

    public MyVirtualGamingApiTests()
    {
        _api = new MyVirtualGamingApi();
        _league = new MyVirtualGamingLeagueProvider().Leagues.First(l => l.Id == MyVirtualGamingLeagueProvider.VGNHL.Id);
        (_league.Info as MyVirtualGamingLeagueInfo)!.SeasonId = 72;
        (_league.Info as MyVirtualGamingLeagueInfo)!.ScheduleId = 119;
    }

    [Fact]
    public void Supports_MyVirtualGaming()
    {
        Assert.True(_api.Supports.Contains(MyVirtualGamingSiteProvider.VGHL.Id), $"{_api.GetType().Name} does not support site {MyVirtualGamingSiteProvider.VGHL.Id}");
    }

    [Fact]
    public async Task GetLeagueInfoAsync_ReturnsExpectedLeagueInfo()
    {
        var league = await _api.GetLeagueAsync(_league);
        Assert.True(league != null, $"{_api.GetType().Name} does not support league {_league.Id}");
        Assert.True(_league.Name == league!.Name, $"expected Name to be {_league.Name} but got {league.Name}");
        Assert.True(_league.LogoUrl == league.LogoUrl, $"expected LogoUrl to be {_league.LogoUrl} but got {league.LogoUrl}");
        Assert.True(league.Info is MyVirtualGamingLeagueInfo, $"expected Info to be of type {typeof(MyVirtualGamingLeagueInfo).Name} but got {league.Info?.GetType().Name ?? "null"}");

        var expectedInfo = JsonSerializer.Deserialize<MyVirtualGamingLeagueInfo>(File.ReadAllText(@"./Files/info.json"))!;
        var actualInfo = (league.Info as MyVirtualGamingLeagueInfo)!;
        Assert.True(expectedInfo.LeagueId == actualInfo.LeagueId, $"expected Info.LeagueId to be {expectedInfo.LeagueId} but got {actualInfo.LeagueId}");
        Assert.True(expectedInfo.SeasonId <= actualInfo.SeasonId, $"expected Info.SeasonId to be greater than or equal to {expectedInfo.SeasonId} but got {actualInfo.SeasonId}");
        Assert.True(expectedInfo.ScheduleId <= actualInfo.ScheduleId, $"expected Info.ScheduleId to be greater than or equal to {expectedInfo.ScheduleId} but got {actualInfo.ScheduleId}");
        Assert.True(expectedInfo.PlayoffEndpoint == actualInfo.PlayoffEndpoint, $"expected Info.PlayoffEndpoint to be {expectedInfo.PlayoffEndpoint} but got {actualInfo.PlayoffEndpoint}");
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsExpectedTeams()
    {
        var actualTeams = await _api.GetTeamsAsync(_league);
        Assert.True(actualTeams != null, $"{_api.GetType().Name} does not support league {_league.Id}");

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
        var actualGames = await _api.GetGamesAsync(_league);
        Assert.True(actualGames != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        var expectedGames = JsonSerializer.Deserialize<IEnumerable<Game>>(File.ReadAllText(@"./Files/games.json"))!;
        var expectedGameCount = expectedGames.Count();
        var actualGameCount = actualGames!.Count();
        Assert.True(expectedGameCount == actualGameCount, $"expected {expectedGameCount} games but found {actualGameCount}");

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

    [Fact]
    public async Task GetBidsAsync_ReturnsNotNull()
    {
        var bids = await _api.GetBidsAsync(_league);
        Assert.True(bids != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        foreach (var bid in bids!)
        {
            Assert.True(_league.Id == bid.LeagueId, $"expected LeagueId to be {_league.Id} but got {bid.LeagueId}");
            Assert.True(int.TryParse(bid.TeamId, out var t), $"expected TeamId to be numeric but got {bid.TeamId}");
            Assert.True(int.TryParse(bid.PlayerId, out var p), $"expected PlayerId to be numeric but got {bid.PlayerId}");
            Assert.True(!string.IsNullOrWhiteSpace(bid.PlayerName), $"expected PlayerName to be non-empty but got {bid.PlayerName}");
            Assert.True(bid.Amount > 0, $"expected Amount to be greater than 0 but got {bid.Amount}");
            Assert.True(BidState.Won == bid.State, $"expected State to be {BidState.Won} but got {bid.State}");
        }
    }

    [Fact]
    public async Task GetContractsAsync_ReturnsNotNull()
    {
        var contracts = await _api.GetContractsAsync(_league);
        Assert.True(contracts != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        foreach (var contract in contracts!)
        {
            Assert.True(_league.Id == contract.LeagueId, $"expected LeagueId to be {_league.Id} but got {contract.LeagueId}");
            Assert.True(int.TryParse(contract.TeamId, out var t), $"expected TeamId to be numeric but got {contract.TeamId}");
            Assert.True(!string.IsNullOrWhiteSpace(contract.PlayerName), $"expected PlayerName to be non-empty but got {contract.PlayerName}");
            Assert.True(contract.Length > 0, $"expected Length to be greater than 0 but got {contract.Length}");
            Assert.True(contract.Amount > 0, $"expected Amount to be greater than 0 but got {contract.Amount}");
        }
    }

    [Fact]
    public async Task GetTradesAsync_ReturnsNotNull()
    {
        var trades = await _api.GetTradesAsync(_league);
        Assert.True(trades != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        foreach (var trade in trades!)
        {
            Assert.True(_league.Id == trade.LeagueId, $"expected LeagueId to be {_league.Id} but got {trade.LeagueId}");
            Assert.True(int.TryParse(trade.FromId, out var f), $"expected FromId to be numeric but got {trade.FromId}");
            Assert.True(int.TryParse(trade.ToId, out var t), $"expected ToId to be numeric but got {trade.ToId}");
            Assert.True(trade.FromAssets.Count() == 1, $"expected exactly 1 Asset but got {trade.FromAssets.Count()}");
        }
    }

    [Fact]
    public async Task GetDraftPicksAsync_ReturnsNotNull()
    {
        var draftPicks = await _api.GetDraftPicksAsync(_league);
        Assert.True(draftPicks != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        foreach (var draftPick in draftPicks!)
        {
            Assert.True(_league.Id == draftPick.LeagueId, $"expected LeagueId to be {_league.Id} but got {draftPick.LeagueId}");
            Assert.True(int.TryParse(draftPick.TeamId, out var f), $"expected TeamId to be numeric but got {draftPick.TeamId}");
            Assert.True(int.TryParse(draftPick.PlayerId, out var t), $"expected PlayerId to be numeric but got {draftPick.PlayerId}");
            Assert.True(!string.IsNullOrWhiteSpace(draftPick.PlayerName), $"expected PlayerName to be non-empty but got {draftPick.PlayerName}");
            Assert.True(draftPick.RoundNumber >= 1, $"expected RoundNumber to be greater than or equal to 1 but got {draftPick.RoundNumber}");
            Assert.True(draftPick.RoundPick >= 1, $"expected RoundPick to be greater than or equal to 1 but got {draftPick.RoundPick}");
            Assert.True(draftPick.OverallPick >= 1, $"expected OverallPick to be greater than or equal to 1 but got {draftPick.OverallPick}");
            Assert.True(draftPick.Timestamp == null, $"expected Timestamp to be null byt got {draftPick.Timestamp}");
        }
    }

    [Fact]
    public async Task GetRosterTransactionsAsync_ReturnsNotNull()
    {
        var rosterTransactions = await _api.GetRosterTransactionsAsync(_league);
        Assert.True(rosterTransactions != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        foreach (var rosterTransaction in rosterTransactions!)
        {
            Assert.True(_league.Id == rosterTransaction.LeagueId, $"expected LeagueId to be {_league.Id} but got {rosterTransaction.LeagueId}");
            Assert.True(rosterTransaction.TeamIds.Count() > 0, $"expected at least one TeamId got {rosterTransaction.TeamIds.Count()}");
            Assert.True(rosterTransaction.PlayerNames.Count() > 0, $"expected at least one PlayerName got {rosterTransaction.PlayerNames.Count()}");
        }
    }
}