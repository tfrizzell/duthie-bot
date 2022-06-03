using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Duthie.Modules.TheSpnhl.Tests")]
namespace Duthie.Modules.TheSpnhl;

internal class TheSpnhlLeagueInfo
{
    public string LeagueType { get; set; } = "";
    public int SeasonId { get; set; }
}