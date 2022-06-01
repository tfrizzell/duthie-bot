namespace Duthie.Types.Api;

public interface ILeagueInfoApi : IApi
{
    Task<ILeague> GetLeagueInfoAsync(League league);
}