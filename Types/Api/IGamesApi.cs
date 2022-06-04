using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface IGamesApi : IApi
{
    Task<IEnumerable<Game>?> GetGamesAsync(League league);
}