using System.Globalization;
using LifeRPG.Core.Models;
using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Repositories;

public sealed class NpcRepository
{
    public void EnsureSeedData()
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();

        if (GetCount(connection, "NpcDefinitions") > 0)
        {
            return;
        }

        var now = DateTime.Now;
        var items = new[]
        {
            new NpcDefinition
            {
                Id = Guid.NewGuid(),
                Name = "档案馆管理员",
                RealIdentity = "Obsidian / 知识系统",
                Category = "System",
                InfluenceType = "positive",
                LinkedDomain = "幽暗城档案馆",
                CreatedAt = now,
                UpdatedAt = now
            },
            new NpcDefinition
            {
                Id = Guid.NewGuid(),
                Name = "深渊低语者",
                RealIdentity = "短视频 / 即时刺激",
                Category = "Temptation",
                InfluenceType = "risky",
                LinkedDomain = "深渊裂隙",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        foreach (var item in items)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO NpcDefinitions (Id, Name, RealIdentity, Category, InfluenceType, LinkedDomain, CreatedAt, UpdatedAt)
                VALUES (@Id, @Name, @RealIdentity, @Category, @InfluenceType, @LinkedDomain, @CreatedAt, @UpdatedAt);
                """;
            command.Parameters.AddWithValue("@Id", item.Id.ToString());
            command.Parameters.AddWithValue("@Name", item.Name);
            command.Parameters.AddWithValue("@RealIdentity", item.RealIdentity);
            command.Parameters.AddWithValue("@Category", item.Category);
            command.Parameters.AddWithValue("@InfluenceType", item.InfluenceType);
            command.Parameters.AddWithValue("@LinkedDomain", item.LinkedDomain);
            command.Parameters.AddWithValue("@CreatedAt", item.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("@UpdatedAt", item.UpdatedAt.ToString("O"));
            command.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<NpcRelationshipSnapshot> GetRelationships(Guid characterId)
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();
        EnsureSeedData();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT d.Id, d.Name, d.RealIdentity, d.Category, d.InfluenceType, d.LinkedDomain, d.CreatedAt, d.UpdatedAt,
                   r.RelationshipValue, r.LastInteractionAt
            FROM NpcDefinitions d
            LEFT JOIN NpcRelationships r
              ON r.NpcDefinitionId = d.Id
             AND r.CharacterId = @CharacterId
            ORDER BY d.Name COLLATE NOCASE ASC;
            """;
        command.Parameters.AddWithValue("@CharacterId", characterId.ToString());

        using var reader = command.ExecuteReader();
        var items = new List<NpcRelationshipSnapshot>();
        while (reader.Read())
        {
            items.Add(new NpcRelationshipSnapshot
            {
                Definition = new NpcDefinition
                {
                    Id = Guid.Parse(reader.GetString(0)),
                    Name = reader.GetString(1),
                    RealIdentity = reader.GetString(2),
                    Category = reader.GetString(3),
                    InfluenceType = reader.GetString(4),
                    LinkedDomain = reader.GetString(5),
                    CreatedAt = DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                    UpdatedAt = DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                },
                RelationshipValue = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                LastInteractionAt = reader.IsDBNull(9)
                    ? null
                    : DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            });
        }

        return items;
    }

    private static int GetCount(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM {tableName};";
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
