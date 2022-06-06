using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface ILeagueInfoApi : ISiteApi
{
    Task<ILeague?> GetLeagueInfoAsync(League league);
}