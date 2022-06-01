namespace Duthie.Types.Api;

public interface ITeamsApi : IApi
{
    Task<IEnumerable<Team>> GetTeamsAsync(League league);
}