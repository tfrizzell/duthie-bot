using Duthie.Types.Leagues;
using Duthie.Types.Teams;

namespace Duthie.Modules.MyVirtualGaming.Tests;

public class MyVirtualGamingApiTests
{
    private readonly IReadOnlyCollection<string> EXCLUDED_TEAMS = new string[] { "Sabres", "Flames", "Blue Jackets", "Kraken" };

    private static readonly Guid TEST_LEAGUE_ID = new Guid("5957b164-7bb5-4324-967a-16c3044260b2");
    private const int TEST_SEASON_ID = 72;
    private const string EXPECTED_LEAGUE_ID = "vgnhl";

    private readonly MyVirtualGamingApi _api;
    private readonly League _league;

    public MyVirtualGamingApiTests()
    {
        _api = new MyVirtualGamingApi();
        _league = new MyVirtualGamingLeagueProvider().Leagues.First(l => l.Id == TEST_LEAGUE_ID);
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
        Assert.True(_league.Name.Equals(league!.Name), $"expected Name to be {_league.Name} but got {league.Name}");
        Assert.True(league?.Info is MyVirtualGamingLeagueInfo, $"expected Info to be of type {typeof(MyVirtualGamingLeagueInfo).Name} but got {league?.Info?.GetType()?.Name ?? "null"}");

        var info = (league?.Info as MyVirtualGamingLeagueInfo)!;
        Assert.True(EXPECTED_LEAGUE_ID.Equals(info.LeagueId), $"expected Info.LeagueId to be {EXPECTED_LEAGUE_ID} but got {info.LeagueId}");
        Assert.True(TEST_SEASON_ID <= info.SeasonId, $"expected Info.SeasonId to be greater than or equal to {TEST_SEASON_ID} but got {info.SeasonId}");
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsExpectedTeams()
    {
        (_league.Info as MyVirtualGamingLeagueInfo)!.SeasonId = TEST_SEASON_ID;
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

            var iidIsInt = int.TryParse(leagueTeam?.IId, out var internalId);
            Assert.True(iidIsInt && internalId > 0, $"expected IId to be integer greater than 0 but got {leagueTeam?.IId}");
        }
    }
}