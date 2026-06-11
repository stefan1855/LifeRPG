using LifeRPG.Core.Rpg;
using LifeRPG.Data.Repositories;
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

        using var connection = CreateConnection();
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

            CREATE TABLE IF NOT EXISTS ActionTasks (
                Id TEXT NOT NULL PRIMARY KEY,
                Title TEXT NOT NULL,
                Content TEXT NOT NULL,
                Category TEXT NOT NULL,
                Status TEXT NOT NULL,
                Quadrant TEXT NOT NULL,
                GrowthAttribute TEXT NOT NULL,
                Experience INTEGER NOT NULL,
                DueAt TEXT NULL,
                CompletedAt TEXT NULL,
                CompletionNote TEXT NOT NULL,
                LinkedLifeRecordId TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS ActionTaskImages (
                Id TEXT NOT NULL PRIMARY KEY,
                ActionTaskId TEXT NOT NULL,
                OriginalFileName TEXT NOT NULL,
                FileName TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                RelativePath TEXT NOT NULL,
                FileExtension TEXT NOT NULL,
                FileSizeBytes INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_ActionTaskImages_ActionTaskId
            ON ActionTaskImages(ActionTaskId);
            """;

        command.ExecuteNonQuery();

        var migrationService = new DatabaseMigrationService();
        migrationService.ApplyMigrations(connection);

        var characterRepository = new CharacterRepository(new RpgEngine());
        characterRepository.GetOrCreateDefaultCharacter();

        var equipmentRepository = new EquipmentRepository();
        equipmentRepository.EnsureSeedData();

        var skillRepository = new SkillRepository();
        skillRepository.EnsureSeedData();

        var npcRepository = new NpcRepository();
        npcRepository.EnsureSeedData();

        var mapRepository = new MapRepository();
        mapRepository.EnsureSeedData();
    }

    public static SqliteConnection CreateConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        return new SqliteConnection(builder.ToString());
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
