using Duthie.Types.Api.Types;
using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface IContractApi : ISiteApi
{
    Task<IEnumerable<Contract>?> GetContractsAsync(League league);
}