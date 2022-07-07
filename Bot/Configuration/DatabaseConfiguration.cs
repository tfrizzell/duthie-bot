namespace Duthie.Bot.Configuration;

public class DatabaseConfiguration
{
    public DatabaseType Type { get; set; } = DatabaseType.Sqlite;
    public string ConnectionString { get; set; } = "Data Source=duthie-bot.db";
}

public enum DatabaseType
{
    Sqlite,
    Mysql
}