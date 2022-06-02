using Duthie.Types.Api;

namespace Duthie.Modules.TheSpnhl;

public class TheSpnhlApi : IApi
{
    private readonly HttpClient _httpClient;

    public TheSpnhlApi()
    {
        _httpClient = new HttpClient();
    }

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { TheSpnhlSiteProvider.SITE_ID };
    }
}