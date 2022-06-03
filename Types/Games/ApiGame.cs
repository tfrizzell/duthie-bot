namespace Duthie.Types.Games;

public class ApiGame
{
    public Guid Id { get; set; }
    public Guid LeagueId { get; set; }
    public string GameId { get; set; } = "";
    public DateTimeOffset Date { get; set; }
    public string VisitorIId { get; set; } = "";
    public int? VisitorScore { get; set; }
    public string HomeIId { get; set; } = "";
    public int? HomeScore { get; set; }
    public bool? Overtime { get; set; }
    public bool? Shootout { get; set; }
}