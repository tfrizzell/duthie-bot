namespace Duthie.Types.Leagues;

public interface ILeagueProvider
{
    IReadOnlyCollection<League> Leagues { get; }
}