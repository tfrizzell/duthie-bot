namespace Duthie.Modules.Leaguegaming.Tests;

public class LeaguegamingSiteProviderTests
{
    [Fact]
    public void Provides_Only_DefaultSites()
    {
        var expectedCount = new LeaguegamingSiteProvider().Sites.Count();
        var actualCount = DefaultSites().Count();
        Assert.True(expectedCount == actualCount, $"expected {expectedCount} sites but found {actualCount}");
    }

    [Theory]
    [MemberData(nameof(DefaultSites))]
    public void DefaultSites_Exist(string id, string name, string url)
    {
        var siteId = new Guid(id);
        var siteProvider = new LeaguegamingSiteProvider();
        var site = siteProvider.Sites.FirstOrDefault(s => s.Id == siteId);

        Assert.True(site != null, $"no matching site found");
        Assert.True(name == site!.Name, $"expected Name to be {name} but got {site.Name}");
        Assert.True(url == site!.Url, $"expected Url to be {url} but got {site.Url}");
        Assert.True(site.Enabled, $"expected Enabled to be {true} but got {site.Enabled}");

        var count = siteProvider.Sites.Count(s => s.Name == name);
        Assert.True(count == 1, $"expected 1 site with name {name} but found {count}");
    }

    public static IEnumerable<object[]> DefaultSites()
    {
        yield return new object[] { "e3f25028-0a34-4430-a2a5-a1a7fab73b41", "Leaguegaming", "www.leaguegaming.com" };
    }
}