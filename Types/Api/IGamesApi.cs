using Duthie.Types.Games;
using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface IGamesApi : IApi
{
    Task<IEnumerable<ApiGame>?> GetGamesAsync(League league);
}