namespace Duthie.Types.Modules.Data;

public abstract class DailyStar
{
    public Guid LeagueId { get; set; }
    public string TeamId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string Position { get; set; } = "";
    public int Rank { get; set; }
    public DateTimeOffset? Timestamp { get; set; }

    public virtual string GetStatLine() => "";
}

public abstract class DailyStarSkater : DailyStar
{
    public int Points => Goals + Assists;
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int PlusMinus { get; set; }

    public override string GetStatLine() => $"{Points} P / {Goals} G / {Assists} A / {(PlusMinus >= 0 ? "+" : "")}{PlusMinus}";
}

public class DailyStarForward : DailyStarSkater
{
    public new string Position { get; } = "Forward";
}

public class DailyStarDefense : DailyStarSkater
{
    public new string Position { get; } = "Defense";
}

public class DailyStarGoalie : DailyStar
{
    public new string Position { get; } = "Goalie";
    public decimal SavePct => Saves / ShotsAgainst;
    public decimal GoalsAgainstAvg { get; set; }
    public int Saves { get; set; }
    public int ShotsAgainst { get; set; }

    public override string GetStatLine() => $"{SavePct.ToString("0.000")} SV% / {GoalsAgainstAvg.ToString("0.00")} GAA / {Saves} SV / {ShotsAgainst} SA";
}