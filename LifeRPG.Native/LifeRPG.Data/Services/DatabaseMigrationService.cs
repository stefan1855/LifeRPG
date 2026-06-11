using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Services;

public sealed class DatabaseMigrationService
{
    private readonly IReadOnlyList<DatabaseMigration> _migrations =
    [
        new(
            "20260605_001_RpgFoundation",
            "RPG foundation tables",
            """
            CREATE TABLE IF NOT EXISTS SchemaMigrations (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                AppliedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS RpgExperienceEvents (
                Id TEXT NOT NULL PRIMARY KEY,
                SourceType TEXT NOT NULL,
                SourceId TEXT NOT NULL,
                CharacterId TEXT NOT NULL,
                TotalExperience INTEGER NOT NULL,
                Note TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_RpgExperienceEvents_Source
            ON RpgExperienceEvents(SourceType, SourceId);
            """),
        new(
            "20260605_002_Character",
            "Character tables",
            """
            CREATE TABLE IF NOT EXISTS Characters (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                Level INTEGER NOT NULL,
                TotalExperience INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CharacterAttributes (
                Id TEXT NOT NULL PRIMARY KEY,
                CharacterId TEXT NOT NULL,
                AttributeKey TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                Level INTEGER NOT NULL,
                Experience INTEGER NOT NULL,
                BaseValue INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS IX_CharacterAttributes_Character_Attribute
            ON CharacterAttributes(CharacterId, AttributeKey);

            CREATE TABLE IF NOT EXISTS AttributeLogs (
                Id TEXT NOT NULL PRIMARY KEY,
                CharacterId TEXT NOT NULL,
                AttributeKey TEXT NOT NULL,
                ExperienceDelta INTEGER NOT NULL,
                SourceType TEXT NOT NULL,
                SourceId TEXT NULL,
                Reason TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_AttributeLogs_Character_CreatedAt
            ON AttributeLogs(CharacterId, CreatedAt DESC);
            """),
        new(
            "20260605_003_EquipmentSkillNpcMap",
            "Equipment, skill, NPC, and map tables",
            """
            CREATE TABLE IF NOT EXISTS EquipmentDefinitions (
                Id TEXT NOT NULL PRIMARY KEY,
                RealItemName TEXT NOT NULL,
                GameItemName TEXT NOT NULL,
                Category TEXT NOT NULL,
                Status TEXT NOT NULL,
                RealEffect TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CharacterEquipment (
                Id TEXT NOT NULL PRIMARY KEY,
                CharacterId TEXT NOT NULL,
                EquipmentDefinitionId TEXT NOT NULL,
                Status TEXT NOT NULL,
                EquippedAt TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE,
                FOREIGN KEY (EquipmentDefinitionId) REFERENCES EquipmentDefinitions(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS EquipmentAttributeBonuses (
                Id TEXT NOT NULL PRIMARY KEY,
                EquipmentDefinitionId TEXT NOT NULL,
                AttributeKey TEXT NOT NULL,
                BonusValue INTEGER NOT NULL,
                FOREIGN KEY (EquipmentDefinitionId) REFERENCES EquipmentDefinitions(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS SkillDefinitions (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                Category TEXT NOT NULL,
                Description TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS CharacterSkills (
                Id TEXT NOT NULL PRIMARY KEY,
                CharacterId TEXT NOT NULL,
                SkillDefinitionId TEXT NOT NULL,
                Level INTEGER NOT NULL,
                Experience INTEGER NOT NULL,
                IsUnlocked INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE,
                FOREIGN KEY (SkillDefinitionId) REFERENCES SkillDefinitions(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS SkillUnlockConditions (
                Id TEXT NOT NULL PRIMARY KEY,
                SkillDefinitionId TEXT NOT NULL,
                ConditionType TEXT NOT NULL,
                ConditionValue TEXT NOT NULL,
                FOREIGN KEY (SkillDefinitionId) REFERENCES SkillDefinitions(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS NpcDefinitions (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                RealIdentity TEXT NOT NULL,
                Category TEXT NOT NULL,
                InfluenceType TEXT NOT NULL,
                LinkedDomain TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS NpcRelationships (
                Id TEXT NOT NULL PRIMARY KEY,
                CharacterId TEXT NOT NULL,
                NpcDefinitionId TEXT NOT NULL,
                RelationshipValue INTEGER NOT NULL,
                LastInteractionAt TEXT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL,
                FOREIGN KEY (CharacterId) REFERENCES Characters(Id) ON DELETE CASCADE,
                FOREIGN KEY (NpcDefinitionId) REFERENCES NpcDefinitions(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS MapLocations (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                LocationType TEXT NOT NULL,
                Description TEXT NOT NULL,
                IsUnlocked INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );
            """),
        new(
            "20260605_004_CharacterProfile",
            "Character profile fields",
            ApplyCharacterProfileMigration)
    ];

    public void ApplyMigrations(SqliteConnection connection)
    {
        EnsureSchemaMigrationsTable(connection);

        foreach (var migration in _migrations)
        {
            if (HasMigrationBeenApplied(connection, migration.Id))
            {
                continue;
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                if (!string.IsNullOrWhiteSpace(migration.Sql))
                {
                    using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = migration.Sql;
                    command.ExecuteNonQuery();
                }

                migration.Apply?.Invoke(connection, transaction);

                using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText =
                    """
                    INSERT INTO SchemaMigrations (Id, Name, AppliedAt)
                    VALUES (@Id, @Name, @AppliedAt);
                    """;
                insertCommand.Parameters.AddWithValue("@Id", migration.Id);
                insertCommand.Parameters.AddWithValue("@Name", migration.Name);
                insertCommand.Parameters.AddWithValue("@AppliedAt", DateTime.Now.ToString("O"));
                insertCommand.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new InvalidOperationException($"数据库迁移失败：{migration.Id} - {migration.Name}。{ex.Message}", ex);
            }
        }
    }

    private static void EnsureSchemaMigrationsTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS SchemaMigrations (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL,
                AppliedAt TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private static bool HasMigrationBeenApplied(SqliteConnection connection, string migrationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM SchemaMigrations WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", migrationId);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void ApplyCharacterProfileMigration(SqliteConnection connection, SqliteTransaction transaction)
    {
        AddColumnIfMissing(connection, transaction, "Characters", "AvatarImagePath", "TEXT");
        AddColumnIfMissing(connection, transaction, "Characters", "Description", "TEXT");
        AddColumnIfMissing(connection, transaction, "Characters", "CreatedAt", "TEXT");
        AddColumnIfMissing(connection, transaction, "Characters", "LastGrowthAt", "TEXT");
    }

    private static void AddColumnIfMissing(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        string columnName,
        string columnDefinition)
    {
        if (ColumnExists(connection, transaction, tableName, columnName))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
        command.ExecuteNonQuery();
    }

    private static bool ColumnExists(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string tableName,
        string columnName)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record DatabaseMigration
    {
        public DatabaseMigration(string id, string name, string sql)
        {
            Id = id;
            Name = name;
            Sql = sql;
        }

        public DatabaseMigration(string id, string name, Action<SqliteConnection, SqliteTransaction> apply)
        {
            Id = id;
            Name = name;
            Apply = apply;
        }

        public string Id { get; }

        public string Name { get; }

        public string? Sql { get; }

        public Action<SqliteConnection, SqliteTransaction>? Apply { get; }
    }
}
