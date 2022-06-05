using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface ITeamApi : ISiteApi
{
    Task<IEnumerable<LeagueTeam>?> GetTeamsAsync(League league);
}