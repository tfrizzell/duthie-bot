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

    [Description("Sends draft picks to your server")]
    Draft,

    [Description("Sends game results to your server")]
    Games,

    [Description("")]
    News,

    [Description("Sends roster transactions to your server")]
    Roster,

    [Description("Sends trades to your server")]
    Trades,

    [Description("")]
    Waivers,
}