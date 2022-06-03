using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface INewsApi : IApi
{
    Task GetNewsAsync(League league);
}