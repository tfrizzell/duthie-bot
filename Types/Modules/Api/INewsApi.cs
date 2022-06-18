using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface INewsApi : ISiteApi
{
    Task<IEnumerable<News>?> GetNewsAsync(League league);

    public string? GetNewsUrl(League league, News news) =>
        null;
}