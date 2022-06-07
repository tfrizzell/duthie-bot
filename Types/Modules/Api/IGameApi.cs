using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface IGameApi : ISiteApi
{
    Task<IEnumerable<Game>?> GetGamesAsync(League league);

    public string? GetGameUrl(League league, Game game) =>
        null;
}