using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface ITradeApi : ISiteApi
{
    Task<IEnumerable<Trade>?> GetTradesAsync(League league);

    public string? GetTradeUrl(League league, Trade trade) =>
        null;
}