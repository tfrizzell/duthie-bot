namespace Duthie.Modules.Leaguegaming.Tests;

public class LeaguegamingLeagueProviderTests
{
    [Fact]
    public void Provides_Only_DefaultLeagues()
    {
        var expectedCount = new LeaguegamingLeagueProvider().Leagues.Count();
        var actualCount = DefaultLeagues().Count();
        Assert.True(expectedCount == actualCount, $"expected {expectedCount} leagues but found {actualCount}");
    }

    [Theory]
    [MemberData(nameof(DefaultLeagues))]
    public void DefaultLeagues_Exist(Guid id, string name, int leagueId, bool enabled = true)
    {
        var leagueProvider = new LeaguegamingLeagueProvider();
        var league = leagueProvider.Leagues.FirstOrDefault(l => l.Id == id);

        Assert.True(league != null, $"league {id} not found");
        Assert.True(LeaguegamingSiteProvider.Leaguegaming.Id == league!.SiteId, $"expected SiteId to be {LeaguegamingSiteProvider.Leaguegaming.Id} but got {league.SiteId}");
        Assert.True(name == league.Name, $"expected Name to be {name} but got {league.Name}");
        Assert.True(league.Info is LeaguegamingLeagueInfo, $"expected Info to be of type {typeof(LeaguegamingLeagueInfo).Name} but got {league.Info?.GetType().Name ?? "null"}");
        Assert.True(league.Enabled == enabled, $"expected Enabled to be {enabled} but got {league.Enabled}");

        var actualLeagueId = (league.Info as LeaguegamingLeagueInfo)!.LeagueId;
        Assert.True(leagueId == actualLeagueId, $"expected Info.LeagueId to be {leagueId} but got {actualLeagueId}");

        var count = leagueProvider.Leagues.Count(s => s.Name == name);
        Assert.True(count == 1, $"expected 1 league with name {name} but found {count}");
    }

    public static IEnumerable<object[]> DefaultLeagues()
    {
        yield return new object[] { new Guid("25e5037d-cf8c-4a36-852c-e3cec36a5dc5"), "LGHL", 37 };
        yield return new object[] { new Guid("981d1b21-fa47-4979-9684-13336ecb3f6c"), "LGAHL", 38 };
        yield return new object[] { new Guid("5dc830f1-84b4-4e87-b6ce-7034fbd4b420"), "LGECHL", 84 };
        yield return new object[] { new Guid("f5bbe441-7cc0-4de8-8960-b479113997b7"), "LGCHL", 39 };
        yield return new object[] { new Guid("73bff1b6-8177-44f8-878a-84b9f74eb728"), "LGHL NCAA", 112 };
        yield return new object[] { new Guid("86c4e0fe-056b-450c-9a55-9ab32946ea31"), "LGHL PSN", 67, false };
        yield return new object[] { new Guid("c5884f38-cae4-461c-af99-beebcdc63e88"), "LGAHL PSN", 68, false };
        yield return new object[] { new Guid("e6f88d50-c9e3-43f2-be3d-11c29fc4403b"), "LGCHL PSN", 69, false };
        yield return new object[] { new Guid("bff63f00-6cb6-4bea-a5ce-5abc3077fc96"), "LGHL NCAA PSN", 113, false };
        yield return new object[] { new Guid("aef1cea7-c626-42b4-9a45-0b9ea3deeb51"), "ESHL", 90, false };
        yield return new object[] { new Guid("0f9b50f8-3526-4bd3-9323-60b67f6a6abb"), "ESHL PSN", 91, false };
        yield return new object[] { new Guid("92718d97-8d2d-4ea3-a4b0-c4cefb75979d"), "LG World Cup", 97, false };
        yield return new object[] { new Guid("76f28c43-fe50-4d66-910d-be37622ecb0e"), "LGFNP", 78 };
        yield return new object[] { new Guid("f8ef5453-6b84-4ae9-9c3e-0553f0fd8971"), "LGFNP PSN", 79, false };
        yield return new object[] { new Guid("c0fcd9f5-d48a-465f-867b-905bafec917d"), "LGBA XBOX", 50, false };
        yield return new object[] { new Guid("3b5133d0-8801-4b86-9920-b7025cf88335"), "LGBA", 70 };
        yield return new object[] { new Guid("f9351c11-a36d-4069-804b-e0f317935576"), "LGFA XBOX", 53, false };
        yield return new object[] { new Guid("1112ece0-a84c-4dc1-9a75-278d4a0e4dd8"), "LGFA", 73 };
    }
}