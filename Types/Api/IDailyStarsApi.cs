namespace Duthie.Types.Api;

public interface IDailyStarsApi : IApi
{
    Task GetDailyStarsAsync(League league);
}