using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface IDailyStarApi : ISiteApi
{
    Task<IEnumerable<DailyStar>?> GetDailyStarsAsync(League league, DateTimeOffset? timestamp = null);

    public string? GetDailyStarsUrl(League league, DailyStar dailyStar) =>
        null;
}