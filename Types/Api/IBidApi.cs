using Duthie.Types.Api.Types;
using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface IBidApi : ISiteApi
{
    Task<IEnumerable<Bid>?> GetBidsAsync(League league);
}