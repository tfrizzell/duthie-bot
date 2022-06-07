using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface ITeamApi : ISiteApi
{
    Task<IEnumerable<Team>?> GetTeamsAsync(League league);
}