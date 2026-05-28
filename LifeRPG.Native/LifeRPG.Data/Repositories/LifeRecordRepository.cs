using LifeRPG.Core.Models;
using LifeRPG.Data.Services;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace LifeRPG.Data.Repositories;

public sealed class LifeRecordRepository
{
    public void Create(LifeRecord record)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO LifeRecords (Id, Title, Content, Category, Experience, CreatedAt, UpdatedAt)
            VALUES (@Id, @Title, @Content, @Category, @Experience, @CreatedAt, @UpdatedAt);
            """;
        command.Parameters.AddWithValue("@Id", record.Id);
        command.Parameters.AddWithValue("@Title", record.Title);
        command.Parameters.AddWithValue("@Content", record.Content);
        command.Parameters.AddWithValue("@Category", record.Category);
        command.Parameters.AddWithValue("@Experience", record.Experience);
        command.Parameters.AddWithValue("@CreatedAt", record.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@UpdatedAt", record.UpdatedAt.ToString("O"));

        command.ExecuteNonQuery();
    }

    public List<LifeRecord> GetAll()
    {
        return Search(null, null, null, null);
    }

    public LifeRecord? GetById(string id)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, Content, Category, Experience, CreatedAt, UpdatedAt
            FROM LifeRecords
            WHERE Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public void Update(LifeRecord record)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE LifeRecords
            SET Title = @Title,
                Content = @Content,
                Category = @Category,
                Experience = @Experience,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", record.Id);
        command.Parameters.AddWithValue("@Title", record.Title);
        command.Parameters.AddWithValue("@Content", record.Content);
        command.Parameters.AddWithValue("@Category", record.Category);
        command.Parameters.AddWithValue("@Experience", record.Experience);
        command.Parameters.AddWithValue("@UpdatedAt", record.UpdatedAt.ToString("O"));

        var affectedRows = command.ExecuteNonQuery();
        if (affectedRows == 0)
        {
            throw new InvalidOperationException($"未找到要更新的行为记录：{record.Id}");
        }
    }

    public void Delete(string id)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM LifeRecords WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    public List<LifeRecord> Search(string? keyword, string? category, DateTime? startDate, DateTime? endDate)
    {
        var records = new List<LifeRecord>();
        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        var normalizedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        var inclusiveEndDate = endDate?.Date.AddDays(1).AddTicks(-1);

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            conditions.Add("(Title LIKE @Keyword OR Content LIKE @Keyword OR Category LIKE @Keyword)");
            command.Parameters.AddWithValue("@Keyword", $"%{normalizedKeyword}%");
        }

        if (!string.IsNullOrWhiteSpace(normalizedCategory))
        {
            conditions.Add("Category = @Category");
            command.Parameters.AddWithValue("@Category", normalizedCategory);
        }

        if (startDate.HasValue)
        {
            conditions.Add("CreatedAt >= @StartDate");
            command.Parameters.AddWithValue("@StartDate", startDate.Value.ToString("O"));
        }

        if (inclusiveEndDate.HasValue)
        {
            conditions.Add("CreatedAt <= @EndDate");
            command.Parameters.AddWithValue("@EndDate", inclusiveEndDate.Value.ToString("O"));
        }

        command.CommandText =
            $"""
            SELECT Id, Title, Content, Category, Experience, CreatedAt, UpdatedAt
            FROM LifeRecords
            {(conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : string.Empty)}
            ORDER BY CreatedAt DESC;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            records.Add(Map(reader));
        }

        return records;
    }

    public List<string> GetAllCategories()
    {
        var categories = new List<string>();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT DISTINCT Category
            FROM LifeRecords
            WHERE Category IS NOT NULL
              AND TRIM(Category) <> ''
            ORDER BY Category COLLATE NOCASE ASC;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            categories.Add(reader.GetString(0));
        }

        return categories;
    }

    private static SqliteConnection CreateConnection()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabaseInitializer.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        return new SqliteConnection(builder.ToString());
    }

    private static LifeRecord Map(SqliteDataReader reader)
    {
        return new LifeRecord
        {
            Id = reader.GetString(0),
            Title = reader.GetString(1),
            Content = reader.GetString(2),
            Category = reader.GetString(3),
            Experience = reader.GetInt32(4),
            CreatedAt = DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            UpdatedAt = DateTime.Parse(reader.GetString(6), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }
}
