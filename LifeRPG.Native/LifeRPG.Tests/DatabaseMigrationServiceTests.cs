using LifeRPG.Data.Services;
using Microsoft.Data.Sqlite;

namespace LifeRPG.Tests;

public sealed class DatabaseMigrationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DatabaseMigrationService _service;

    public DatabaseMigrationServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _service = new DatabaseMigrationService();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public void ApplyMigrations_CreatesAllFourMigrations()
    {
        _service.ApplyMigrations(_connection);

        var applied = GetAppliedMigrationIds();
        Assert.Equal(4, applied.Count);
        Assert.Contains("20260605_001_RpgFoundation", applied);
        Assert.Contains("20260605_002_Character", applied);
        Assert.Contains("20260605_003_EquipmentSkillNpcMap", applied);
        Assert.Contains("20260605_004_CharacterProfile", applied);
    }

    [Fact]
    public void ApplyMigrations_IsIdempotent()
    {
        _service.ApplyMigrations(_connection);
        var firstCount = GetAppliedMigrationCount();

        // Run again — should not double-apply
        _service.ApplyMigrations(_connection);
        var secondCount = GetAppliedMigrationCount();

        Assert.Equal(firstCount, secondCount);
        Assert.Equal(4, secondCount);
    }

    [Fact]
    public void Migration001_CreatesRpgFoundationTables()
    {
        _service.ApplyMigrations(_connection);

        Assert.True(TableExists("SchemaMigrations"));
        Assert.True(TableExists("RpgExperienceEvents"));
    }

    [Fact]
    public void Migration001_CreatesUniqueIndexOnExperienceEvents()
    {
        _service.ApplyMigrations(_connection);

        var indexes = GetIndexes("RpgExperienceEvents");
        Assert.Contains(indexes, i => i.Contains("IX_RpgExperienceEvents_Source"));
    }

    [Fact]
    public void Migration002_CreatesCharacterTables()
    {
        _service.ApplyMigrations(_connection);

        Assert.True(TableExists("Characters"));
        Assert.True(TableExists("CharacterAttributes"));
        Assert.True(TableExists("AttributeLogs"));
    }

    [Fact]
    public void Migration002_CreatesCharacterAttributeUniqueIndex()
    {
        _service.ApplyMigrations(_connection);

        var indexes = GetIndexes("CharacterAttributes");
        Assert.Contains(indexes, i => i.Contains("IX_CharacterAttributes_Character_Attribute"));
    }

    [Fact]
    public void Migration003_CreatesExtendedSystemTables()
    {
        _service.ApplyMigrations(_connection);

        Assert.True(TableExists("EquipmentDefinitions"));
        Assert.True(TableExists("CharacterEquipment"));
        Assert.True(TableExists("EquipmentAttributeBonuses"));
        Assert.True(TableExists("SkillDefinitions"));
        Assert.True(TableExists("CharacterSkills"));
        Assert.True(TableExists("SkillUnlockConditions"));
        Assert.True(TableExists("NpcDefinitions"));
        Assert.True(TableExists("NpcRelationships"));
        Assert.True(TableExists("MapLocations"));
    }

    [Fact]
    public void Migration004_AddsCharacterProfileColumns()
    {
        _service.ApplyMigrations(_connection);

        var columns = GetColumns("Characters");
        Assert.Contains(columns, c => c == "AvatarImagePath");
        Assert.Contains(columns, c => c == "Description");
        Assert.Contains(columns, c => c == "LastGrowthAt");
    }

    [Fact]
    public void Migration004_BackfillLogic_DoesNotCrashOnFreshDatabase()
    {
        // Run all migrations including 004
        _service.ApplyMigrations(_connection);

        // Insert a character — CreatedAt is populated by migration 002's NOT NULL
        using var insertCmd = _connection.CreateCommand();
        insertCmd.CommandText =
            """
            INSERT INTO Characters (Id, Name, Level, TotalExperience, CreatedAt, UpdatedAt)
            VALUES ('test-char', '测试', 1, 0, '2025-01-01T00:00:00.0000000', '2025-01-01T00:00:00.0000000');
            """;
        insertCmd.ExecuteNonQuery();

        // Verify the character was inserted correctly
        using var checkCmd = _connection.CreateCommand();
        checkCmd.CommandText = "SELECT Name, CreatedAt FROM Characters WHERE Id = 'test-char';";
        using var reader = checkCmd.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("测试", reader.GetString(0));
        Assert.NotNull(reader.GetString(1));
    }

    private List<string> GetAppliedMigrationIds()
    {
        var ids = new List<string>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id FROM SchemaMigrations ORDER BY Id;";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            ids.Add(reader.GetString(0));
        }
        return ids;
    }

    private int GetAppliedMigrationCount()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM SchemaMigrations;";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private bool TableExists(string tableName)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = @Name;";
        cmd.Parameters.AddWithValue("@Name", tableName);
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    private List<string> GetIndexes(string tableName)
    {
        var indexes = new List<string>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'index' AND tbl_name = @Name;";
        cmd.Parameters.AddWithValue("@Name", tableName);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            indexes.Add(reader.GetString(0));
        }
        return indexes;
    }

    private List<string> GetColumns(string tableName)
    {
        var columns = new List<string>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName});";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            columns.Add(reader.GetString(1));
        }
        return columns;
    }
}
