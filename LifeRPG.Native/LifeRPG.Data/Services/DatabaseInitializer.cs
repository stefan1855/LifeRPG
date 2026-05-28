using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Services;

public static class DatabaseInitializer
{
    public const string DatabaseFileName = "liferpg.db";

    public static string DatabasePath => Path.Combine(GetDataDirectory(), DatabaseFileName);

    public static void Initialize()
    {
        Directory.CreateDirectory(GetRootDirectory());
        Directory.CreateDirectory(GetDataDirectory());

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        using var connection = new SqliteConnection(builder.ToString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS LifeRecords (
                Id TEXT NOT NULL PRIMARY KEY,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                Category TEXT NOT NULL,
                Experience INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS LifeRecordImages (
                Id TEXT NOT NULL PRIMARY KEY,
                LifeRecordId TEXT NOT NULL,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (LifeRecordId) REFERENCES LifeRecords(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS GrowthAttributes (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                Value INTEGER NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """;

        command.ExecuteNonQuery();
    }

    private static string GetRootDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LifeRPG.Native");
    }

    private static string GetDataDirectory()
    {
        return Path.Combine(GetRootDirectory(), "data");
    }
}
