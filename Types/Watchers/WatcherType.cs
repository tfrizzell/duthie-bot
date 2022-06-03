using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Duthie.Types.Watchers;

public enum WatcherType
{
    [Description("Tracks and announces winning bids")]
    Bids,

    [Description("Tracks and announces new contracts")]
    Contracts,

    [Display(Name = "Daily Stars")]
    [Description("Tracks and announces daily stars")]
    DailyStars,

    [Display(Name = "Draft Picks")]
    [Description("Tracks and announces new draft picks")]
    DraftPicks,

    [Description("Tracks and announces game results")]
    Games,

    [Description("Tracks and announces news items not covered by other types")]
    News,

    [Description("Tracks and announces trades")]
    Trades,

    [Description("Tracks and announces players placed on, or claimed off, waivers")]
    Waivers
}