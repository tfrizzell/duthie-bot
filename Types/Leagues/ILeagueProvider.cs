namespace Duthie.Types;

public interface ILeagueProvider
{
    IReadOnlyCollection<League> Leagues { get; }
}