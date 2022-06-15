using Duthie.Types.Modules.Data;
using League = Duthie.Types.Leagues.League;

namespace Duthie.Types.Modules.Api;

public interface IRosterApi : ISiteApi
{
    Task<IEnumerable<RosterTransaction>?> GetRosterTransactionsAsync(League league);

    public string? GetRosterTransactionUrl(League league, RosterTransaction rosterTransaction) =>
        null;
}