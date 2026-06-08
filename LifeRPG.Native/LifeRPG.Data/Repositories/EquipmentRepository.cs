using System.Globalization;
using LifeRPG.Core.Models;
using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Repositories;

public sealed class EquipmentRepository
{
    public void EnsureSeedData()
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();

        var count = GetCount(connection, "EquipmentDefinitions");
        if (count > 0)
        {
            return;
        }

        var now = DateTime.Now;
        var items = new[]
        {
            new EquipmentDefinition
            {
                Id = Guid.NewGuid(),
                RealItemName = "Obsidian",
                GameItemName = "灵魂档案石板",
                Category = "研究装备",
                Status = "Equipped",
                RealEffect = "支持知识整理、复盘和系统搭建",
                CreatedAt = now,
                UpdatedAt = now
            },
            new EquipmentDefinition
            {
                Id = Guid.NewGuid(),
                RealItemName = "主力电脑",
                GameItemName = "档案馆主机",
                Category = "研究装备",
                Status = "Equipped",
                RealEffect = "支持学习、开发和内容产出",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        foreach (var item in items)
        {
            InsertDefinition(connection, item);
        }
    }

    public IReadOnlyList<EquipmentDefinition> GetEquippedItems(Guid characterId)
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();
        EnsureSeedData();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, RealItemName, GameItemName, Category, Status, RealEffect, CreatedAt, UpdatedAt
            FROM EquipmentDefinitions
            WHERE Status IN ('Equipped', 'Owned')
            ORDER BY UpdatedAt DESC;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<EquipmentDefinition>();
        while (reader.Read())
        {
            items.Add(Map(reader));
        }

        return items;
    }

    private static void InsertDefinition(SqliteConnection connection, EquipmentDefinition item)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO EquipmentDefinitions (
                Id, RealItemName, GameItemName, Category, Status, RealEffect, CreatedAt, UpdatedAt)
            VALUES (
                @Id, @RealItemName, @GameItemName, @Category, @Status, @RealEffect, @CreatedAt, @UpdatedAt);
            """;
        command.Parameters.AddWithValue("@Id", item.Id.ToString());
        command.Parameters.AddWithValue("@RealItemName", item.RealItemName);
        command.Parameters.AddWithValue("@GameItemName", item.GameItemName);
        command.Parameters.AddWithValue("@Category", item.Category);
        command.Parameters.AddWithValue("@Status", item.Status);
        command.Parameters.AddWithValue("@RealEffect", item.RealEffect);
        command.Parameters.AddWithValue("@CreatedAt", item.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@UpdatedAt", item.UpdatedAt.ToString("O"));
        command.ExecuteNonQuery();
    }

    private static int GetCount(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(1) FROM {tableName};";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static EquipmentDefinition Map(SqliteDataReader reader)
    {
        return new EquipmentDefinition
        {
            Id = Guid.Parse(reader.GetString(0)),
            RealItemName = reader.GetString(1),
            GameItemName = reader.GetString(2),
            Category = reader.GetString(3),
            Status = reader.GetString(4),
            RealEffect = reader.GetString(5),
            CreatedAt = DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            UpdatedAt = DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }
}
