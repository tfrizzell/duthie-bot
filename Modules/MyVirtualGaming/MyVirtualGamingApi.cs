using Duthie.Types.Api;

namespace Duthie.Modules.MyVirtualGaming;

public class MyVirtualGamingApi : IApi
{
    private readonly HttpClient _httpClient;

    public MyVirtualGamingApi()
    {
        _httpClient = new HttpClient();
    }

    public IReadOnlySet<Guid> Supports
    {
        get => new HashSet<Guid> { MyVirtualGamingSiteProvider.SITE_ID };
    }
}