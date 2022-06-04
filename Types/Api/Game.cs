namespace Duthie.Types.Api;

public class Game
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public ulong GameId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string VisitorExternalId { get; set; } = "";
    public int? VisitorScore { get; set; }
    public string HomeExternalId { get; set; } = "";
    public int? HomeScore { get; set; }
    public bool? Overtime { get; set; }
    public bool? Shootout { get; set; }
}