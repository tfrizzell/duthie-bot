using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface ILeagueApi : ISiteApi
{
    Task<ILeague?> GetLeagueAsync(League league);
}