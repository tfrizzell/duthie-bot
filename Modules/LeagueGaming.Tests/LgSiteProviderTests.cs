namespace Duthie.Modules.LeagueGaming.Tests;

public class LgSiteProviderTests
{
    [Fact]
    public void Provides_Only_DefaultSites()
    {
        var expectedCount = new LgSiteProvider().Sites.Count();
        var actualCount = DefaultSites().Count();
        Assert.True(expectedCount == actualCount, $"expected {expectedCount} sites but found {actualCount}");
    }

    [Theory]
    [MemberData(nameof(DefaultSites))]
    public void DefaultSites_Exist(string id, string name)
    {
        var siteId = new Guid(id);
        var siteProvider = new LgSiteProvider();
        var site = siteProvider.Sites.FirstOrDefault(s => s.Id == LgSiteProvider.SITE_ID);

        Assert.True(site != null, $"no matching site found");
        Assert.True(name.Equals(site?.Name), $"expected Name to be {name} but got {site?.Name}");
        Assert.True(site?.Enabled, $"expected Enabled to be {true} but got {site?.Enabled}");

        var count = siteProvider.Sites.Count(s => s.Name.Equals(name));
        Assert.True(count == 1, $"expected 1 site with name {name} but found {count}");
    }

    internal static IEnumerable<object[]> DefaultSites()
    {
        yield return new object[] { "e3f25028-0a34-4430-a2a5-a1a7fab73b41", "leaguegaming.com" };
    }
}