using Duthie.Types.Api.Data;
using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface IGameApi : ISiteApi
{
    Task<IEnumerable<Game>?> GetGamesAsync(League league);

    public string? GetGameUrl(League league, Game game) =>
        null;
}