using Duthie.Types.Leagues;

namespace Duthie.Types.Api;

public interface IBidsApi : IApi
{
    Task<IEnumerable<Bid>?> GetBidsAsync(League league);
}