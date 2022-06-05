using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Duthie.Modules.MyVirtualGaming.Tests")]
namespace Duthie.Modules.MyVirtualGaming;

internal class MyVirtualGamingLeagueInfo
{
    public string LeagueId { get; set; } = "";
    public int SeasonId { get; set; }
    public int ScheduleId { get; set; }
}