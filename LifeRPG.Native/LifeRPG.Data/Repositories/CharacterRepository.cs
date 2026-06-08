using System.Globalization;
using LifeRPG.Core.Models;
using LifeRPG.Core.Rpg;
using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Repositories;

public sealed class CharacterRepository
{
    private readonly RpgEngine _rpgEngine;

    public CharacterRepository()
        : this(new RpgEngine())
    {
    }

    public CharacterRepository(RpgEngine rpgEngine)
    {
        _rpgEngine = rpgEngine;
    }

    public Character GetOrCreateDefaultCharacter()
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();

        var existing = GetDefaultCharacter(connection);
        if (existing is not null)
        {
            EnsureDefaultAttributes(connection, existing.Id);
            return existing;
        }

        var now = DateTime.Now;
        var character = new Character
        {
            Id = Guid.NewGuid(),
            Name = "亡灵术士",
            Level = 1,
            TotalExperience = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        using var transaction = connection.BeginTransaction();

        using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO Characters (Id, Name, Level, TotalExperience, CreatedAt, UpdatedAt)
                VALUES (@Id, @Name, @Level, @TotalExperience, @CreatedAt, @UpdatedAt);
                """;
            command.Parameters.AddWithValue("@Id", character.Id.ToString());
            command.Parameters.AddWithValue("@Name", character.Name);
            command.Parameters.AddWithValue("@Level", character.Level);
            command.Parameters.AddWithValue("@TotalExperience", character.TotalExperience);
            command.Parameters.AddWithValue("@CreatedAt", character.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("@UpdatedAt", character.UpdatedAt.ToString("O"));
            command.ExecuteNonQuery();
        }

        EnsureDefaultAttributes(connection, transaction, character.Id);
        transaction.Commit();
        return character;
    }

    public IReadOnlyList<CharacterAttribute> GetAttributes(Guid characterId)
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();
        EnsureDefaultAttributes(connection, characterId);

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, CharacterId, AttributeKey, DisplayName, Level, Experience, BaseValue, CreatedAt, UpdatedAt
            FROM CharacterAttributes
            WHERE CharacterId = @CharacterId
            ORDER BY CASE AttributeKey
                WHEN 'physique' THEN 0
                WHEN 'mind' THEN 1
                WHEN 'discipline' THEN 2
                WHEN 'expression' THEN 3
                WHEN 'creativity' THEN 4
                WHEN 'resources' THEN 5
                WHEN 'general' THEN 6
                ELSE 7
            END, DisplayName COLLATE NOCASE ASC;
            """;
        command.Parameters.AddWithValue("@CharacterId", characterId.ToString());

        using var reader = command.ExecuteReader();
        var items = new List<CharacterAttribute>();
        while (reader.Read())
        {
            items.Add(MapAttribute(reader));
        }

        return items;
    }

    public IReadOnlyList<AttributeLog> GetRecentAttributeLogs(Guid characterId, int take = 20)
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, CharacterId, AttributeKey, ExperienceDelta, SourceType, SourceId, Reason, CreatedAt
            FROM AttributeLogs
            WHERE CharacterId = @CharacterId
            ORDER BY CreatedAt DESC
            LIMIT @Take;
            """;
        command.Parameters.AddWithValue("@CharacterId", characterId.ToString());
        command.Parameters.AddWithValue("@Take", Math.Max(1, take));

        using var reader = command.ExecuteReader();
        var items = new List<AttributeLog>();
        while (reader.Read())
        {
            items.Add(MapLog(reader));
        }

        return items;
    }

    public CharacterDashboardSnapshot GetDashboardSnapshot()
    {
        var character = GetOrCreateDefaultCharacter();
        var levelResult = _rpgEngine.CalculateLevel(character.TotalExperience);

        return new CharacterDashboardSnapshot
        {
            Character = character,
            ExperienceToNextLevel = levelResult.ExperienceToNextLevel,
            ExperienceIntoCurrentLevel = levelResult.ExperienceIntoCurrentLevel,
            CurrentLevelExperienceSpan = Math.Max(1, levelResult.ExperienceRequiredForNextLevel - levelResult.ExperienceRequiredForCurrentLevel),
            Attributes = GetAttributes(character.Id),
            RecentAttributeLogs = GetRecentAttributeLogs(character.Id),
            EquippedItems = new EquipmentRepository().GetEquippedItems(character.Id),
            Skills = new SkillRepository().GetUnlockedSkills(character.Id),
            NpcRelationships = new NpcRepository().GetRelationships(character.Id),
            MapLocations = new MapRepository().GetLocations()
        };
    }

    public void AddExperience(Guid characterId, string attributeKey, int experienceDelta, string sourceType, Guid? sourceId, string reason)
    {
        ApplyExperienceEvent(
            characterId,
            sourceType,
            sourceId ?? Guid.NewGuid(),
            experienceDelta,
            [new RpgAttributeGain
            {
                AttributeKey = CharacterAttributeCatalog.NormalizeKey(attributeKey),
                DisplayName = CharacterAttributeCatalog.Resolve(attributeKey).DisplayName,
                ExperienceDelta = experienceDelta
            }],
            reason);
    }

    public RpgExperienceResult ApplyExperienceEvent(
        Guid characterId,
        string sourceType,
        Guid sourceId,
        int totalExperience,
        IReadOnlyList<RpgAttributeGain> gains,
        string reason)
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();
        EnsureDefaultAttributes(connection, characterId);

        if (HasExperienceEvent(connection, sourceType, sourceId))
        {
            return new RpgExperienceResult
            {
                Applied = false,
                IsDuplicate = true,
                TotalExperienceGranted = 0,
                Gains = []
            };
        }

        using var transaction = connection.BeginTransaction();
        var now = DateTime.Now;

        var character = GetById(connection, transaction, characterId)
            ?? throw new InvalidOperationException($"未找到角色：{characterId}");

        character.TotalExperience += Math.Max(0, totalExperience);
        var levelResult = _rpgEngine.CalculateLevel(character.TotalExperience);
        character.Level = levelResult.Level;
        character.UpdatedAt = now;
        UpdateCharacter(connection, transaction, character);

        foreach (var gain in gains.Where(item => item.ExperienceDelta > 0))
        {
            var attribute = GetOrCreateAttribute(connection, transaction, characterId, gain);
            attribute.Experience += gain.ExperienceDelta;
            attribute.Level = _rpgEngine.CalculateLevel(attribute.Experience).Level;
            attribute.UpdatedAt = now;
            UpdateAttribute(connection, transaction, attribute);
            InsertAttributeLog(connection, transaction, characterId, gain, sourceType, sourceId, reason, now);
        }

        InsertExperienceEvent(connection, transaction, characterId, sourceType, sourceId, totalExperience, reason, now);
        transaction.Commit();

        return new RpgExperienceResult
        {
            Applied = true,
            IsDuplicate = false,
            TotalExperienceGranted = Math.Max(0, totalExperience),
            Gains = gains
        };
    }

    public void RecalculateCharacterLevel(Guid characterId)
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();
        var character = GetById(connection, null, characterId);
        if (character is null)
        {
            return;
        }

        var levelResult = _rpgEngine.CalculateLevel(character.TotalExperience);
        character.Level = levelResult.Level;
        character.UpdatedAt = DateTime.Now;
        UpdateCharacter(connection, null, character);
    }

    private static Character? GetDefaultCharacter(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Name, Level, TotalExperience, CreatedAt, UpdatedAt
            FROM Characters
            ORDER BY CreatedAt ASC
            LIMIT 1;
            """;

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapCharacter(reader) : null;
    }

    private static Character? GetById(SqliteConnection connection, SqliteTransaction? transaction, Guid characterId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            SELECT Id, Name, Level, TotalExperience, CreatedAt, UpdatedAt
            FROM Characters
            WHERE Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", characterId.ToString());

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapCharacter(reader) : null;
    }

    private static bool HasExperienceEvent(SqliteConnection connection, string sourceType, Guid sourceId)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(1)
            FROM RpgExperienceEvents
            WHERE SourceType = @SourceType
              AND SourceId = @SourceId;
            """;
        command.Parameters.AddWithValue("@SourceType", sourceType);
        command.Parameters.AddWithValue("@SourceId", sourceId.ToString());
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    private static void EnsureDefaultAttributes(SqliteConnection connection, Guid characterId)
    {
        EnsureDefaultAttributes(connection, null, characterId);
    }

    private static void EnsureDefaultAttributes(SqliteConnection connection, SqliteTransaction? transaction, Guid characterId)
    {
        foreach (var definition in CharacterAttributeCatalog.GetDefaultDefinitions())
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO CharacterAttributes (
                    Id, CharacterId, AttributeKey, DisplayName, Level, Experience, BaseValue, CreatedAt, UpdatedAt)
                SELECT @Id, @CharacterId, @AttributeKey, @DisplayName, 1, 0, @BaseValue, @CreatedAt, @UpdatedAt
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM CharacterAttributes
                    WHERE CharacterId = @CharacterId
                      AND AttributeKey = @AttributeKey
                );
                """;
            command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("@CharacterId", characterId.ToString());
            command.Parameters.AddWithValue("@AttributeKey", definition.Key);
            command.Parameters.AddWithValue("@DisplayName", definition.DisplayName);
            command.Parameters.AddWithValue("@BaseValue", definition.BaseValue);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("O"));
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("O"));
            command.ExecuteNonQuery();
        }
    }

    private static CharacterAttribute GetOrCreateAttribute(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid characterId,
        RpgAttributeGain gain)
    {
        var key = CharacterAttributeCatalog.NormalizeKey(gain.AttributeKey);

        using (var query = connection.CreateCommand())
        {
            query.Transaction = transaction;
            query.CommandText =
                """
                SELECT Id, CharacterId, AttributeKey, DisplayName, Level, Experience, BaseValue, CreatedAt, UpdatedAt
                FROM CharacterAttributes
                WHERE CharacterId = @CharacterId
                  AND AttributeKey = @AttributeKey
                LIMIT 1;
                """;
            query.Parameters.AddWithValue("@CharacterId", characterId.ToString());
            query.Parameters.AddWithValue("@AttributeKey", key);

            using var reader = query.ExecuteReader();
            if (reader.Read())
            {
                return MapAttribute(reader);
            }
        }

        var createdAt = DateTime.Now;
        var attribute = new CharacterAttribute
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            AttributeKey = key,
            DisplayName = string.IsNullOrWhiteSpace(gain.DisplayName) ? CharacterAttributeCatalog.GeneralDisplayName : gain.DisplayName,
            Level = 1,
            Experience = 0,
            BaseValue = 0,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText =
            """
            INSERT INTO CharacterAttributes (
                Id, CharacterId, AttributeKey, DisplayName, Level, Experience, BaseValue, CreatedAt, UpdatedAt)
            VALUES (
                @Id, @CharacterId, @AttributeKey, @DisplayName, @Level, @Experience, @BaseValue, @CreatedAt, @UpdatedAt);
            """;
        insert.Parameters.AddWithValue("@Id", attribute.Id.ToString());
        insert.Parameters.AddWithValue("@CharacterId", attribute.CharacterId.ToString());
        insert.Parameters.AddWithValue("@AttributeKey", attribute.AttributeKey);
        insert.Parameters.AddWithValue("@DisplayName", attribute.DisplayName);
        insert.Parameters.AddWithValue("@Level", attribute.Level);
        insert.Parameters.AddWithValue("@Experience", attribute.Experience);
        insert.Parameters.AddWithValue("@BaseValue", attribute.BaseValue);
        insert.Parameters.AddWithValue("@CreatedAt", attribute.CreatedAt.ToString("O"));
        insert.Parameters.AddWithValue("@UpdatedAt", attribute.UpdatedAt.ToString("O"));
        insert.ExecuteNonQuery();

        return attribute;
    }

    private static void UpdateCharacter(SqliteConnection connection, SqliteTransaction? transaction, Character character)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            UPDATE Characters
            SET Name = @Name,
                Level = @Level,
                TotalExperience = @TotalExperience,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", character.Id.ToString());
        command.Parameters.AddWithValue("@Name", character.Name);
        command.Parameters.AddWithValue("@Level", character.Level);
        command.Parameters.AddWithValue("@TotalExperience", character.TotalExperience);
        command.Parameters.AddWithValue("@UpdatedAt", character.UpdatedAt.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static void UpdateAttribute(SqliteConnection connection, SqliteTransaction transaction, CharacterAttribute attribute)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            UPDATE CharacterAttributes
            SET DisplayName = @DisplayName,
                Level = @Level,
                Experience = @Experience,
                BaseValue = @BaseValue,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", attribute.Id.ToString());
        command.Parameters.AddWithValue("@DisplayName", attribute.DisplayName);
        command.Parameters.AddWithValue("@Level", attribute.Level);
        command.Parameters.AddWithValue("@Experience", attribute.Experience);
        command.Parameters.AddWithValue("@BaseValue", attribute.BaseValue);
        command.Parameters.AddWithValue("@UpdatedAt", attribute.UpdatedAt.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static void InsertAttributeLog(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid characterId,
        RpgAttributeGain gain,
        string sourceType,
        Guid sourceId,
        string reason,
        DateTime createdAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO AttributeLogs (
                Id, CharacterId, AttributeKey, ExperienceDelta, SourceType, SourceId, Reason, CreatedAt)
            VALUES (
                @Id, @CharacterId, @AttributeKey, @ExperienceDelta, @SourceType, @SourceId, @Reason, @CreatedAt);
            """;
        command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@CharacterId", characterId.ToString());
        command.Parameters.AddWithValue("@AttributeKey", CharacterAttributeCatalog.NormalizeKey(gain.AttributeKey));
        command.Parameters.AddWithValue("@ExperienceDelta", gain.ExperienceDelta);
        command.Parameters.AddWithValue("@SourceType", sourceType);
        command.Parameters.AddWithValue("@SourceId", sourceId.ToString());
        command.Parameters.AddWithValue("@Reason", reason);
        command.Parameters.AddWithValue("@CreatedAt", createdAt.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static void InsertExperienceEvent(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid characterId,
        string sourceType,
        Guid sourceId,
        int totalExperience,
        string note,
        DateTime createdAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO RpgExperienceEvents (
                Id, SourceType, SourceId, CharacterId, TotalExperience, Note, CreatedAt)
            VALUES (
                @Id, @SourceType, @SourceId, @CharacterId, @TotalExperience, @Note, @CreatedAt);
            """;
        command.Parameters.AddWithValue("@Id", Guid.NewGuid().ToString());
        command.Parameters.AddWithValue("@SourceType", sourceType);
        command.Parameters.AddWithValue("@SourceId", sourceId.ToString());
        command.Parameters.AddWithValue("@CharacterId", characterId.ToString());
        command.Parameters.AddWithValue("@TotalExperience", Math.Max(0, totalExperience));
        command.Parameters.AddWithValue("@Note", note);
        command.Parameters.AddWithValue("@CreatedAt", createdAt.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static Character MapCharacter(SqliteDataReader reader)
    {
        return new Character
        {
            Id = Guid.Parse(reader.GetString(0)),
            Name = reader.GetString(1),
            Level = reader.GetInt32(2),
            TotalExperience = reader.GetInt32(3),
            CreatedAt = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            UpdatedAt = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }

    private static CharacterAttribute MapAttribute(SqliteDataReader reader)
    {
        return new CharacterAttribute
        {
            Id = Guid.Parse(reader.GetString(0)),
            CharacterId = Guid.Parse(reader.GetString(1)),
            AttributeKey = reader.GetString(2),
            DisplayName = reader.GetString(3),
            Level = reader.GetInt32(4),
            Experience = reader.GetInt32(5),
            BaseValue = reader.GetInt32(6),
            CreatedAt = DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            UpdatedAt = DateTime.Parse(reader.GetString(8), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }

    private static AttributeLog MapLog(SqliteDataReader reader)
    {
        return new AttributeLog
        {
            Id = Guid.Parse(reader.GetString(0)),
            CharacterId = Guid.Parse(reader.GetString(1)),
            AttributeKey = reader.GetString(2),
            ExperienceDelta = reader.GetInt32(3),
            SourceType = reader.GetString(4),
            SourceId = reader.IsDBNull(5) ? null : Guid.Parse(reader.GetString(5)),
            Reason = reader.GetString(6),
            CreatedAt = DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }
}
