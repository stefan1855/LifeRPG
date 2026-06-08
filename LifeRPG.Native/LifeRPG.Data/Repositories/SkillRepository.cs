using System.Globalization;
using LifeRPG.Core.Models;
using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Repositories;

public sealed class SkillRepository
{
    public void EnsureSeedData()
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();

        if (GetCount(connection, "SkillDefinitions") > 0)
        {
            return;
        }

        var now = DateTime.Now;
        var items = new[]
        {
            new SkillDefinition
            {
                Id = Guid.NewGuid(),
                Name = "前端学习",
                Category = "研究",
                Description = "保留技能成长骨架，后续再接解锁与分支。",
                CreatedAt = now,
                UpdatedAt = now
            },
            new SkillDefinition
            {
                Id = Guid.NewGuid(),
                Name = "复盘能力",
                Category = "规划",
                Description = "支持后续复盘与系统规划玩法。",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        foreach (var item in items)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO SkillDefinitions (Id, Name, Category, Description, CreatedAt, UpdatedAt)
                VALUES (@Id, @Name, @Category, @Description, @CreatedAt, @UpdatedAt);
                """;
            command.Parameters.AddWithValue("@Id", item.Id.ToString());
            command.Parameters.AddWithValue("@Name", item.Name);
            command.Parameters.AddWithValue("@Category", item.Category);
            command.Parameters.AddWithValue("@Description", item.Description);
            command.Parameters.AddWithValue("@CreatedAt", item.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("@UpdatedAt", item.UpdatedAt.ToString("O"));
            command.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<SkillDefinition> GetUnlockedSkills(Guid characterId)
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();
        EnsureSeedData();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Name, Category, Description, CreatedAt, UpdatedAt
            FROM SkillDefinitions
            ORDER BY Name COLLATE NOCASE ASC;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<SkillDefinition>();
        while (reader.Read())
        {
            items.Add(new SkillDefinition
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                Description = reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                UpdatedAt = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
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
