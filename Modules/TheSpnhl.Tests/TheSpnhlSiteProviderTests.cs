namespace Duthie.Modules.TheSpnhl.Tests;

public class TheSpnhlSiteProviderTests
{
    [Fact]
    public void Provides_Only_DefaultSites()
    {
        var expectedCount = new TheSpnhlSiteProvider().Sites.Count();
        var actualCount = DefaultSites().Count();
        Assert.True(expectedCount == actualCount, $"expected {expectedCount} sites but found {actualCount}");
    }

    [Theory]
    [MemberData(nameof(DefaultSites))]
    public void DefaultSites_Exist(string id, string name)
    {
        var siteId = new Guid(id);
        var siteProvider = new TheSpnhlSiteProvider();
        var site = siteProvider.Sites.FirstOrDefault(s => s.Id == TheSpnhlSiteProvider.SITE_ID);

        Assert.True(site != null, $"no matching site found");
        Assert.True(name.Equals(site?.Name), $"expected Name to be {name} but got {site?.Name}");
        Assert.True(site?.Enabled, $"expected Enabled to be {true} but got {site?.Enabled}");

        var count = siteProvider.Sites.Count(s => s.Name.Equals(name));
        Assert.True(count == 1, $"expected 1 site with name {name} but found {count}");
    }

    internal static IEnumerable<object[]> DefaultSites()
    {
        yield return new object[] { "c193a2eb-f6fd-4c1d-bf2b-b77ef05f236c", "thespnhl.com" };
    }
}