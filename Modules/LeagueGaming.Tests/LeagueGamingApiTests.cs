using Duthie.Data;
using Duthie.Types;

namespace Duthie.Modules.LeagueGaming.Tests;

public class LeagueGamingApiTests
{
    private static readonly Guid LEAGUE_ID = new Guid("86c4e0fe-056b-450c-9a55-9ab32946ea31");
    private const int EXPECTED_LEAGUE_ID = 67;
    private const int EXPECTED_SEASON_ID = 20;
    private const int EXPECTED_FORUM_ID = 586;

    private readonly LeagueGamingApi _api;
    private readonly League _league;

    public LeagueGamingApiTests()
    {
        _api = new LeagueGamingApi();
        _league = new LeagueGamingLeagueProvider().Leagues.First(l => l.Id == LEAGUE_ID);
    }

    [Fact]
    public void Supports_LeagueGaming()
    {
        Assert.True(_api.Supports.Contains(LeagueGamingSiteProvider.SITE_ID), $"{_api.GetType().Name} does not support site {LeagueGamingSiteProvider.SITE_ID}");
    }

    [Fact]
    public async Task GetLeagueInfoAsync_ReturnsLeagueInfo()
    {
        var league = await _api.GetLeagueInfoAsync(_league);
        Assert.True(league != null, $"{_api.GetType().Name} does not support league {_league.Id}");
        Assert.True(_league.Name.Equals(league!.Name), $"expected Name to be {_league.Name} but got {league.Name}");
        Assert.True(league?.Info is LeagueGamingLeagueInfo, $"expected Info to be of type {typeof(LeagueGamingLeagueInfo).Name} but got {league?.Info?.GetType()?.Name ?? "null"}");

        var info = (league?.Info as LeagueGamingLeagueInfo)!;
        Assert.True(EXPECTED_LEAGUE_ID == info.LeagueId, $"expected Info.LeagueId to be {EXPECTED_LEAGUE_ID} but got {info.LeagueId}");
        Assert.True(EXPECTED_SEASON_ID <= info.SeasonId, $"expected Info.SeasonId to be greater than or equal to {EXPECTED_SEASON_ID} but got {info.SeasonId}");
        Assert.True(EXPECTED_FORUM_ID == info.ForumId, $"expected Info.ForumId to be greater than or equal to {EXPECTED_FORUM_ID} but got {info.ForumId}");
    }

    [Fact]
    public async Task GetTeamsAsync_ReturnsNHLTeams()
    {
        var league = await _api.GetLeagueInfoAsync(_league);
        _league.Name = league?.Name ?? _league.Name;
        _league.Info = league?.Info ?? _league.Info;

        var teams = await _api.GetTeamsAsync(_league);
        Assert.True(teams != null, $"{_api.GetType().Name} does not support league {_league.Id}");

        var expectedCount = DefaultTeams.NHL.Count();
        var actualCount = teams!.Count();
        Assert.True(expectedCount == actualCount, $"expected {expectedCount} teams but found {actualCount}");

        var badLeagueCount = teams!.Count(t => t.LeagueId != _league.Id);
        Assert.True(badLeagueCount == 0, $"expected all teams to have LeagueId {_league.Id} but found {badLeagueCount} that did not");

        foreach (var team in DefaultTeams.NHL)
        {
            var leagueTeam = teams?.FirstOrDefault(t => t.Team.Name.Equals(team.Name) && t.Team.ShortName.Equals(team.ShortName));
            Assert.True(team.Name.Equals(leagueTeam?.Team.Name), $"expected Name {team.Name} but got {leagueTeam?.Team.Name}");
            Assert.True(team.ShortName.Equals(leagueTeam?.Team.ShortName), $"expected ShortName {team.ShortName} but got {leagueTeam?.Team.ShortName}");

            var iidIsInt = int.TryParse(leagueTeam?.IId, out var internalId);
            Assert.True(iidIsInt && internalId > 0, $"expected IId to be integer greater than 0 but got {leagueTeam?.IId}");
        }
    }
}