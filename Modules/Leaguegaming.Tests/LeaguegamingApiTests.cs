using System.Text.Json;
using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Modules.Leaguegaming.Tests;

public class LeaguegamingApiTests
{
    private readonly LeaguegamingApi _api;
    private readonly League _league;

    public LeaguegamingApiTests()
    {
        _api = new LeaguegamingApi();
        _league = new LeaguegamingLeagueProvider().Leagues.First(l => l.Id == LeaguegamingLeagueProvider.LGHL_PSN.Id);
        (_league.Info as LeaguegamingLeagueInfo)!.SeasonId = 19;
        (_league.Info as LeaguegamingLeagueInfo)!.DraftId = 550;
        (_league.Info as LeaguegamingLeagueInfo)!.DraftDate = DateTimeOffset.Parse("2022-06-03 20:00:00 -04:00");
    }

    [Fact]
    public void Supports_Leaguegaming()
    {
        Assert.True(_api.Supports.Contains(LeaguegamingSiteProvider.Leaguegaming.Id), $"{_api.GetType().Name} does not support site {LeaguegamingSiteProvider.Leaguegaming.Id}");
    }

    [Fact]
    public async Task GetLeagueInfoAsync_ReturnsExpectedLeagueInfo()
    {
        var league = await _api.GetLeagueAsync(_league);
        Assert.True(league != null, $"{_api.GetType().Name} does not support league {_league.Id}");
        Assert.True(_league.Name == league!.Name, $"expected Name to be {_league.Name} but got {league.Name}");
        Assert.True(_league.LogoUrl == league.LogoUrl, $"expected LogoUrl to be {_league.LogoUrl} but got {league.LogoUrl}");
        Assert.True(league.Info is LeaguegamingLeagueInfo, $"expected Info to be of type {typeof(LeaguegamingLeagueInfo).Name} but got {league.Info?.GetType().Name ?? "null"}");

        var expectedInfo = JsonSerializer.Deserialize<LeaguegamingLeagueInfo>(File.ReadAllText(@"./Files/info.json"))!;
        var actualInfo = (league.Info as LeaguegamingLeagueInfo)!;
        Assert.True(expectedInfo.LeagueId == actualInfo.LeagueId, $"expected Info.LeagueId to be {expectedInfo.LeagueId} but got {actualInfo.LeagueId}");
        Assert.True(expectedInfo.SeasonId <= actualInfo.SeasonId, $"expected Info.SeasonId to be greater than or equal to {expectedInfo.SeasonId} but got {actualInfo.SeasonId}");
        Assert.True(expectedInfo.ForumId == actualInfo.ForumId, $"expected Info.ForumId to be {expectedInfo.ForumId} but got {actualInfo.ForumId}");
        Assert.True(expectedInfo.DraftId <= actualInfo.DraftId, $"expected Info.DraftId to be greater than or equal to {expectedInfo.DraftId} but got {actualInfo.DraftId}");
        Assert.True(expectedInfo.DraftDate <= actualInfo.DraftDate, $"expected Info.ForumId to be greater than or equal to {expectedInfo.DraftDate} but got {actualInfo.DraftDate}");
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
            Assert.True(expectedTeam.Id == actualTeam!.Id, $"[team {expectedTeam.Id}] expected Id {expectedTeam.Id} but got {actualTeam.Id}");
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
        var expectedTeamCount = expectedGames.Count();
        var actualGameCount = actualGames!.Count();
        Assert.True(expectedTeamCount == actualGameCount, $"expected {expectedTeamCount} games but found {actualGameCount}");

        foreach (var expectedGame in expectedGames)
        {
            var actualGame = actualGames!.FirstOrDefault(g => g.Id == expectedGame.Id);
            Assert.True(actualGame != null, $"[game {expectedGame.Id}] game not found");

            actualGame!.Timestamp = actualGame.Timestamp.AddYears(2022 - actualGame.Timestamp.Year);
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
        }
    }

    [Fact]
    public async Task GetDraftPicksAsync_InThePast_ReturnsNothing()
    {
        (_league.Info as LeaguegamingLeagueInfo)!.DraftDate = DateTimeOffset.UtcNow.AddDays(-7);
        var draftPicks = await _api.GetDraftPicksAsync(_league);
        Assert.True(draftPicks != null, $"{_api.GetType().Name} does not support league {_league.Id}");
        Assert.True(draftPicks!.Count() == 0, $"expected 0 draft picks but got {draftPicks!.Count()}");
    }

    [Fact]
    public async Task GetDraftPicksAsync_InThePresent_ReturnsNotNull()
    {
        (_league.Info as LeaguegamingLeagueInfo)!.DraftDate = DateTimeOffset.UtcNow;
        var actualDraftPicks = await _api.GetDraftPicksAsync(_league);
        Assert.True(actualDraftPicks != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        var expectedDraftPicks = JsonSerializer.Deserialize<IEnumerable<DraftPick>>(File.ReadAllText(@"./Files/draftPicks.json"))!;
        var expectedTeamCount = expectedDraftPicks.Count();
        var actualGameCount = actualDraftPicks!.Count();
        Assert.True(expectedTeamCount == actualGameCount, $"expected {expectedTeamCount} draft picks but found {actualGameCount}");

        var draftId = (_league.Info as LeaguegamingLeagueInfo)!.DraftId;

        foreach (var expectedDraftPick in expectedDraftPicks)
        {
            var actualDraftPick = actualDraftPicks!.FirstOrDefault(d => d.OverallPick == expectedDraftPick.OverallPick);
            Assert.True(actualDraftPick != null, $"[draft {draftId}] pick {expectedDraftPick.OverallPick} not found");
            Assert.True(expectedDraftPick.LeagueId == actualDraftPick!.LeagueId, $"expected LeagueId to be {expectedDraftPick.LeagueId} but got {actualDraftPick.LeagueId}");
            Assert.True(expectedDraftPick.TeamId == actualDraftPick!.TeamId, $"expected TeamId to be {expectedDraftPick.TeamId} but got {actualDraftPick.TeamId}");
            Assert.True(expectedDraftPick.PlayerId == actualDraftPick!.PlayerId, $"expected PlayerId to be {expectedDraftPick.PlayerId} but got {actualDraftPick.PlayerId}");
            Assert.True(!string.IsNullOrWhiteSpace(actualDraftPick.PlayerName), $"expected PlayerName to not be empty but got {actualDraftPick.PlayerName}");
            Assert.True(expectedDraftPick.RoundNumber == actualDraftPick!.RoundNumber, $"expected RoundNumber to be {expectedDraftPick.RoundNumber} but got {actualDraftPick.RoundNumber}");
            Assert.True(expectedDraftPick.RoundPick == actualDraftPick!.RoundPick, $"expected RoundPick to be {expectedDraftPick.RoundPick} but got {actualDraftPick.RoundPick}");
            Assert.True(expectedDraftPick.OverallPick == actualDraftPick!.OverallPick, $"expected LeagueId to be {expectedDraftPick.OverallPick} but got {actualDraftPick.OverallPick}");
            Assert.True(expectedDraftPick.Timestamp == actualDraftPick!.Timestamp, $"expected LeagueId to be {expectedDraftPick.Timestamp} but got {actualDraftPick.Timestamp}");
        }
    }

    [Fact]
    public async Task GetDraftPicksAsync_InTheFuture_ReturnsNothing()
    {
        (_league.Info as LeaguegamingLeagueInfo)!.DraftDate = DateTimeOffset.UtcNow.AddDays(7);
        var draftPicks = await _api.GetDraftPicksAsync(_league);
        Assert.True(draftPicks != null, $"{_api.GetType().Name} does not support league {_league.Id}");
        Assert.True(draftPicks!.Count() == 0, $"expected 0 draft picks but got {draftPicks!.Count()}");
    }

    [Fact]
    public async Task GetWaiversAsync_ReturnsNotNull()
    {
        var waivers = await _api.GetWaiversAsync(_league);
        Assert.True(waivers != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        foreach (var waiver in waivers!)
        {
            Assert.True(_league.Id == waiver.LeagueId, $"expected LeagueId to be {_league.Id} but got {waiver.LeagueId}");
            Assert.True(int.TryParse(waiver.TeamId, out var t), $"expected TeamId to be numeric but got {waiver.TeamId}");
            Assert.True(!string.IsNullOrWhiteSpace(waiver.PlayerName), $"expected PlayerName to be non-empty but got {waiver.PlayerName}");
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
            Assert.True(rosterTransaction.PlayerNames.Count() > 0, $"expected at least one PlayerName got {rosterTransaction.PlayerNames.Count()}");
        }
    }
}