using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface IDailyStarApi : ISiteApi
{
    Task GetDailyStarsAsync(League league);
}