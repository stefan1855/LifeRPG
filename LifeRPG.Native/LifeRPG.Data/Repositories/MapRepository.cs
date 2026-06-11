using System.Globalization;
using LifeRPG.Core.Models;
using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Repositories;

public sealed class MapRepository
{
    public void EnsureSeedData()
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();

        if (GetCount(connection, "MapLocations") > 0)
        {
            return;
        }

        var now = DateTime.Now;
        var items = new[]
        {
            new MapLocation
            {
                Id = Guid.NewGuid(),
                Name = "幽暗城档案馆",
                LocationType = "研究",
                Description = "对应学习、阅读、系统搭建和知识沉淀。",
                IsUnlocked = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new MapLocation
            {
                Id = Guid.NewGuid(),
                Name = "血肉熔炉",
                LocationType = "恢复",
                Description = "对应运动、作息、饮食和身体管理。",
                IsUnlocked = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new MapLocation
            {
                Id = Guid.NewGuid(),
                Name = "深渊裂隙",
                LocationType = "风险",
                Description = "对应高压透支、即时刺激和腐化风险。",
                IsUnlocked = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        foreach (var item in items)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO MapLocations (Id, Name, LocationType, Description, IsUnlocked, CreatedAt, UpdatedAt)
                VALUES (@Id, @Name, @LocationType, @Description, @IsUnlocked, @CreatedAt, @UpdatedAt);
                """;
            command.Parameters.AddWithValue("@Id", item.Id.ToString());
            command.Parameters.AddWithValue("@Name", item.Name);
            command.Parameters.AddWithValue("@LocationType", item.LocationType);
            command.Parameters.AddWithValue("@Description", item.Description);
            command.Parameters.AddWithValue("@IsUnlocked", item.IsUnlocked ? 1 : 0);
            command.Parameters.AddWithValue("@CreatedAt", item.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("@UpdatedAt", item.UpdatedAt.ToString("O"));
            command.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<MapLocation> GetLocations()
    {
        using var connection = LifeRPG.Data.Services.DatabaseInitializer.CreateConnection();
        connection.Open();
        EnsureSeedData();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Name, LocationType, Description, IsUnlocked, CreatedAt, UpdatedAt
            FROM MapLocations
            ORDER BY Name COLLATE NOCASE ASC;
            """;

        using var reader = command.ExecuteReader();
        var items = new List<MapLocation>();
        while (reader.Read())
        {
            items.Add(new MapLocation
            {
                Id = Guid.Parse(reader.GetString(0)),
                Name = reader.GetString(1),
                LocationType = reader.GetString(2),
                Description = reader.GetString(3),
                IsUnlocked = reader.GetInt32(4) == 1,
                CreatedAt = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                UpdatedAt = DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
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
