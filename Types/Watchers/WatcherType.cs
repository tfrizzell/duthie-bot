using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Duthie.Types.Watchers;

public enum WatcherType
{
    [Description("Sends winning bids to your server")]
    Bids,

    [Description("Sends contract signings to your server")]
    Contracts,

    [Display(Name = "Daily Stars")]
    [Description("")]
    DailyStars,

    [Display(Name = "Draft Picks")]
    [Description("Sends draft picks to your server")]
    DraftPicks,

    [Description("Sends game results to your server")]
    Games,

    [Description("")]
    News,

    [Display(Name = "Roster Transactions")]
    [Description("Sends roster transactions to your server")]
    RosterTransactions,

    [Description("Sends trades to your server")]
    Trades,

    [Description("")]
    Waivers,
}