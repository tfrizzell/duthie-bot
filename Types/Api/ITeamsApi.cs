namespace Duthie.Types.Api;

public interface ITeamsApi : IApi
{
    Task<IEnumerable<LeagueTeam>?> GetTeamsAsync(League league);
}