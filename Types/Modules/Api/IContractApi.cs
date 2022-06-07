using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface IContractApi : ISiteApi
{
    Task<IEnumerable<Contract>?> GetContractsAsync(League league);

    public string? GetContractUrl(League league, Contract contract) =>
        null;
}