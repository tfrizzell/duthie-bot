using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface INewsApi : ISiteApi
{
    Task GetNewsAsync(League league);
}