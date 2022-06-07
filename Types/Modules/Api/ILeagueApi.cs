namespace Duthie.Types.Modules.Api;
using League = Duthie.Types.Leagues.League;

public interface ILeagueApi : ISiteApi
{
    Task<Data.League?> GetLeagueAsync(League league);
}