namespace Duthie.Modules.MyVirtualGaming.Tests;

public class MyVirtualGamingLeagueProviderTests
{
    [Fact]
    public void Provides_Only_DefaultLeagues()
    {
        var expectedCount = new MyVirtualGamingLeagueProvider().Leagues.Count();
        var actualCount = DefaultLeagues().Count();
        Assert.True(expectedCount == actualCount, $"expected {expectedCount} leagues but found {actualCount}");
    }

    [Theory]
    [MemberData(nameof(DefaultLeagues))]
    public void DefaultLeagues_Exist(Guid id, string name, string leagueId, bool enabled = true)
    {
        var leagueProvider = new MyVirtualGamingLeagueProvider();
        var league = leagueProvider.Leagues.FirstOrDefault(l => l.Id == id);

        Assert.True(league != null, $"league {id} not found");
        Assert.True(MyVirtualGamingSiteProvider.VGHL.Id == league!.SiteId, $"expected SiteId to be {MyVirtualGamingSiteProvider.VGHL.Id} but got {league.SiteId}");
        Assert.True(name == league.Name, $"expected Name to be {name} but got {league.Name}");
        Assert.True(league.Info is MyVirtualGamingLeagueInfo, $"expected Info to be of type {typeof(MyVirtualGamingLeagueInfo).Name} but got {league.Info?.GetType().Name ?? "null"}");
        Assert.True(league.Enabled == enabled, $"expected Enabled to be {enabled} but got {league.Enabled}");

        var actualLeagueId = (league.Info as MyVirtualGamingLeagueInfo)!.LeagueId;
        Assert.True(leagueId == actualLeagueId, $"expected Info.LeagueId to be {leagueId} but got {actualLeagueId}");

        var count = leagueProvider.Leagues.Count(s => s.Name == name);
        Assert.True(count == 1, $"expected 1 league with name {name} but found {count}");
    }

    internal static IEnumerable<object[]> DefaultLeagues()
    {
        yield return new object[] { new Guid("5957b164-7bb5-4324-967a-16c3044260b2"), "VGNHL National League", "vgnhl" };
        yield return new object[] { new Guid("0fc1b6e9-9181-4545-9d32-5edbd67b276a"), "VGAHL Affiliate League", "vgahl" };
        yield return new object[] { new Guid("ed4403ee-5ed3-46b2-8dce-d245c1e5b132"), "VGPHL Prospect League", "vgphl", false };
        yield return new object[] { new Guid("0ec6177f-7e39-437b-9cb9-1551db76bd4e"), "VGHL World Championship", "vghlwc" };
        yield return new object[] { new Guid("8cba4eb0-8722-4415-aa82-b0027ae33702"), "VGHL Club League", "vghlclub" };
        yield return new object[] { new Guid("9545ede8-6948-44e0-8ef8-61668c6ab9e1"), "VGHL 3s League", "vghl3" };
        yield return new object[] { new Guid("cef6775d-f621-4164-a629-80ec54e016fa"), "VGIHL International League", "vgihl" };
    }
}