namespace Duthie.Modules.TheSpnhl.Tests;

public class TheSpnhlLeagueProviderTests
{
    [Fact]
    public void Provides_Only_DefaultLeagues()
    {
        var expectedCount = new TheSpnhlLeagueProvider().Leagues.Count();
        var actualCount = DefaultLeagues().Count();
        Assert.True(expectedCount == actualCount, $"expected {expectedCount} leagues but found {actualCount}");
    }

    [Theory]
    [MemberData(nameof(DefaultLeagues))]
    public void DefaultLeagues_Exist(Guid id, string name)
    {
        var leagueProvider = new TheSpnhlLeagueProvider();
        var league = leagueProvider.Leagues.FirstOrDefault(l => l.Id == id);

        Assert.True(league != null, $"league {id} not found");
        Assert.True(TheSpnhlSiteProvider.SITE_ID == league!.SiteId, $"expected SiteId to be {TheSpnhlSiteProvider.SITE_ID} but got {league.SiteId}");
        Assert.True(name == league.Name, $"expected Name to be {name} but got {league.Name}");
        Assert.True(league.Enabled, $"expected Enabled to be {true} but got {league.Enabled}");

        var count = leagueProvider.Leagues.Count(s => s.Name == name);
        Assert.True(count == 1, $"expected 1 league with name {name} but found {count}");
    }

    internal static IEnumerable<object[]> DefaultLeagues()
    {
        yield return new object[] { new Guid("6991c990-a4fa-488b-884a-79b00e4e3577"), "SPNHL" };
    }
}