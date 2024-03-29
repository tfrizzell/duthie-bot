using Duthie.Types.Leagues;
using Duthie.Types.Teams;

namespace Duthie.Types.Games;

public class Game
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public ulong GameId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public Guid VisitorId { get; set; }
    public int? VisitorScore { get; set; }
    public Guid HomeId { get; set; }
    public int? HomeScore { get; set; }
    public bool? Overtime { get; set; }
    public bool? Shootout { get; set; }

#nullable disable
    public virtual League League { get; set; }
    public virtual Team VisitorTeam { get; set; }
    public virtual Team HomeTeam { get; set; }
#nullable enable
}