using Duthie.Types.Leagues;
using Duthie.Types.Teams;

namespace Duthie.Modules.LeagueGaming.Tests;

public class LeagueGamingApiTests
{
    private readonly IReadOnlyCollection<string> EXCLUDED_TEAMS = new string[] { };

    private static readonly Guid TEST_LEAGUE_ID = new Guid("86c4e0fe-056b-450c-9a55-9ab32946ea31");
    private const int TEST_SEASON_ID = 19;
    private const int EXPECTED_LEAGUE_ID = 67;
    private const int EXPECTED_FORUM_ID = 586;
    private const int EXPECTED_GAME_COUNT = 1421;
    private static readonly DateTimeOffset EXPECTED_GAMES_START = DateTimeOffset.Parse("2022-02-22 05:00:00 -00:00");
    private static readonly DateTimeOffset EXPECTED_GAMES_END = DateTimeOffset.Parse("2022-05-25 03:59:59 -00:00");

    private readonly LeagueGamingApi _api;
    private readonly League _league;

    public LeagueGamingApiTests()
    {
        _api = new LeagueGamingApi();
        _league = new LeagueGamingLeagueProvider().Leagues.First(l => l.Id == TEST_LEAGUE_ID);
    }

    [Fact]
    public void Supports_LeagueGaming()
    {
        Assert.True(_api.Supports.Contains(LeagueGamingSiteProvider.SITE_ID), $"{_api.GetType().Name} does not support site {LeagueGamingSiteProvider.SITE_ID}");
    }

    [Fact]
    public async Task GetLeagueInfoAsync_ReturnsExpectedLeagueInfo()
    {
        var league = await _api.GetLeagueInfoAsync(_league);
        Assert.True(league != null, $"{_api.GetType().Name} does not support league {_league.Id}");
        Assert.True(_league.Name.Equals(league!.Name), $"expected Name to be {_league.Name} but got {league.Name}");
        Assert.True(league?.Info is LeagueGamingLeagueInfo, $"expected Info to be of type {typeof(LeagueGamingLeagueInfo).Name} but got {league?.Info?.GetType()?.Name ?? "null"}");

        var info = (league?.Info as LeagueGamingLeagueInfo)!;
        Assert.True(EXPECTED_LEAGUE_ID == info.LeagueId, $"expected Info.LeagueId to be {EXPECTED_LEAGUE_ID} but got {info.LeagueId}");
        Assert.True(TEST_SEASON_ID <= info.SeasonId, $"expected Info.SeasonId to be greater than or equal to {TEST_SEASON_ID} but got {info.SeasonId}");
        Assert.True(EXPECTED_FORUM_ID == info.ForumId, $"expected Info.ForumId to be greater than or equal to {EXPECTED_FORUM_ID} but got {info.ForumId}");
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsExpectedTeams()
    {
        (_league.Info as LeagueGamingLeagueInfo)!.SeasonId = TEST_SEASON_ID;
        var teams = await _api.GetTeamsAsync(_league);
        Assert.True(teams != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        var expectedTeamCount = DefaultTeams.NHL.Count(t => !EXCLUDED_TEAMS.Contains(t.Name) && !EXCLUDED_TEAMS.Contains(t.ShortName));
        var actualTeamCount = teams!.Count();
        Assert.True(expectedTeamCount == actualTeamCount, $"expected {expectedTeamCount} teams but found {actualTeamCount}");

        var badLeagueCount = teams!.Count(t => t.LeagueId != _league.Id);
        Assert.True(badLeagueCount == 0, $"expected all teams to have LeagueId {_league.Id} but found {badLeagueCount} that did not");

        foreach (var team in DefaultTeams.NHL)
        {
            if (EXCLUDED_TEAMS.Contains(team.Name) || EXCLUDED_TEAMS.Contains(team.ShortName))
                continue;

            var leagueTeam = teams?.FirstOrDefault(t => t.Team.Name.Equals(team.Name) && t.Team.ShortName.Equals(team.ShortName));
            Assert.True(team.Name.Equals(leagueTeam?.Team.Name), $"expected Name {team.Name} but got {leagueTeam?.Team.Name}");
            Assert.True(team.ShortName.Equals(leagueTeam?.Team.ShortName), $"expected ShortName {team.ShortName} but got {leagueTeam?.Team.ShortName}");

            var iidIsInt = int.TryParse(leagueTeam?.IId, out var internalId);
            Assert.True(iidIsInt && internalId > 0, $"expected IId to be integer greater than 0 but got {leagueTeam?.IId}");
        }
    }

    [Fact]
    public async Task GetGamesAsync_ReturnsExpectedGames()
    {
        (_league.Info as LeagueGamingLeagueInfo)!.SeasonId = TEST_SEASON_ID;
        var games = await _api.GetGamesAsync(_league);
        Assert.True(games != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        var actualGameCount = games!.Count();
        Assert.True(EXPECTED_GAME_COUNT == actualGameCount, $"expected {EXPECTED_GAME_COUNT} teams but found {actualGameCount}");

        var badLeagueCount = games!.Count(t => t.LeagueId != _league.Id);
        Assert.True(badLeagueCount == 0, $"expected all games to have LeagueId {_league.Id} but found {badLeagueCount} that did not");

        foreach (var game in games!)
        {
            game.Date = game.Date.AddYears(2022 - game.Date.Year);
            Assert.True(!string.IsNullOrWhiteSpace(game.GameId), $"expected GameId to be non-empty");
            Assert.True(game.Date >= EXPECTED_GAMES_START, $"expected Date to be on or after {EXPECTED_GAMES_START} but got {game.Date}");
            Assert.True(game.Date <= EXPECTED_GAMES_END, $"expected Date to be on or before {EXPECTED_GAMES_END} but got {game.Date}");
            Assert.True(!string.IsNullOrWhiteSpace(game.VisitorIId), $"expected VisitorIId to be non-empty");
            Assert.True(game.VisitorScore == null || game.VisitorScore >= 0, $"expected VisitorScore to be greater than or equal to 0 but got {game.VisitorScore}");
            Assert.True(!string.IsNullOrWhiteSpace(game.HomeIId), $"expected HomeIId to be non-empty");
            Assert.True(game.HomeScore == null || game.HomeScore >= 0, $"expected HomeScore to be greater than or equal to 0 but got {game.HomeScore}");
            Assert.True(game.Overtime == null, $"expected Overtime to be null but got {game.Overtime}");
            Assert.True(game.Shootout == null, $"expected Shootout to be null but got {game.Shootout}");
        }
    }
}