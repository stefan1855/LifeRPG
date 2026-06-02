using System.Globalization;
using LifeRPG.Core.Models;
using LifeRPG.Data.Services;
using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Repositories;

public sealed class ActionTaskRepository
{
    public void Create(ActionTask task)
    {
        var experience = task.Experience > 0
            ? task.Experience
            : ActionTask.CalculateExperience(task.Quadrant);

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO ActionTasks (
                Id, Title, Content, Category, Status, Quadrant, GrowthAttribute, Experience,
                DueAt, CompletedAt, CompletionNote, LinkedLifeRecordId, CreatedAt, UpdatedAt)
            VALUES (
                @Id, @Title, @Content, @Category, @Status, @Quadrant, @GrowthAttribute, @Experience,
                @DueAt, @CompletedAt, @CompletionNote, @LinkedLifeRecordId, @CreatedAt, @UpdatedAt);
            """;
        AddCommonParameters(command, task, experience);
        command.Parameters.AddWithValue("@Id", task.Id);
        command.Parameters.AddWithValue("@CreatedAt", task.CreatedAt.ToString("O"));

        command.ExecuteNonQuery();
    }

    public void Update(ActionTask task)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE ActionTasks
            SET Title = @Title,
                Content = @Content,
                Category = @Category,
                Status = @Status,
                Quadrant = @Quadrant,
                GrowthAttribute = @GrowthAttribute,
                Experience = @Experience,
                DueAt = @DueAt,
                CompletedAt = @CompletedAt,
                CompletionNote = @CompletionNote,
                LinkedLifeRecordId = @LinkedLifeRecordId,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        AddCommonParameters(command, task, task.Experience);
        command.Parameters.AddWithValue("@Id", task.Id);

        command.ExecuteNonQuery();
    }

    public void Delete(string id)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ActionTasks WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    public ActionTask? GetById(string id)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, Content, Category, Status, Quadrant, GrowthAttribute, Experience,
                   DueAt, CompletedAt, CompletionNote, LinkedLifeRecordId, CreatedAt, UpdatedAt
            FROM ActionTasks
            WHERE Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public List<ActionTask> GetAll()
    {
        return Search(null, null, null, null, null);
    }

    public List<ActionTask> GetByStatus(ActionTaskStatus status)
    {
        return Search(null, null, status, null, null);
    }

    public List<ActionTask> GetTodayTasks()
    {
        var todayStart = DateTime.Today;
        var todayEnd = todayStart.AddDays(1).AddTicks(-1);
        var tasks = new List<ActionTask>();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, Content, Category, Status, Quadrant, GrowthAttribute, Experience,
                   DueAt, CompletedAt, CompletionNote, LinkedLifeRecordId, CreatedAt, UpdatedAt
            FROM ActionTasks
            WHERE DueAt IS NOT NULL
              AND DueAt >= @TodayStart
              AND DueAt <= @TodayEnd
              AND Status <> @CompletedStatus
              AND Status <> @CancelledStatus
            ORDER BY DueAt ASC, CreatedAt DESC;
            """;
        command.Parameters.AddWithValue("@TodayStart", todayStart.ToString("O"));
        command.Parameters.AddWithValue("@TodayEnd", todayEnd.ToString("O"));
        command.Parameters.AddWithValue("@CompletedStatus", ActionTaskStatus.Completed.ToString());
        command.Parameters.AddWithValue("@CancelledStatus", ActionTaskStatus.Cancelled.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tasks.Add(Map(reader));
        }

        return tasks;
    }

    public List<ActionTask> GetOverdueTasks()
    {
        var now = DateTime.Now;
        var tasks = new List<ActionTask>();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, Content, Category, Status, Quadrant, GrowthAttribute, Experience,
                   DueAt, CompletedAt, CompletionNote, LinkedLifeRecordId, CreatedAt, UpdatedAt
            FROM ActionTasks
            WHERE DueAt IS NOT NULL
              AND DueAt < @Now
              AND Status <> @CompletedStatus
              AND Status <> @CancelledStatus
            ORDER BY DueAt ASC, CreatedAt DESC;
            """;
        command.Parameters.AddWithValue("@Now", now.ToString("O"));
        command.Parameters.AddWithValue("@CompletedStatus", ActionTaskStatus.Completed.ToString());
        command.Parameters.AddWithValue("@CancelledStatus", ActionTaskStatus.Cancelled.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tasks.Add(Map(reader));
        }

        return tasks;
    }

    public List<ActionTask> GetCompletedTasks()
    {
        var tasks = new List<ActionTask>();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, Content, Category, Status, Quadrant, GrowthAttribute, Experience,
                   DueAt, CompletedAt, CompletionNote, LinkedLifeRecordId, CreatedAt, UpdatedAt
            FROM ActionTasks
            WHERE Status = @CompletedStatus
            ORDER BY CompletedAt DESC, UpdatedAt DESC;
            """;
        command.Parameters.AddWithValue("@CompletedStatus", ActionTaskStatus.Completed.ToString());

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tasks.Add(Map(reader));
        }

        return tasks;
    }

    public List<ActionTask> Search(
        string? keyword,
        string? category,
        ActionTaskStatus? status,
        DateTime? startDueAt,
        DateTime? endDueAt)
    {
        var tasks = new List<ActionTask>();
        var normalizedKeyword = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        var normalizedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        var inclusiveEndDueAt = endDueAt?.Date.AddDays(1).AddTicks(-1);

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        var conditions = new List<string>();

        if (!string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            conditions.Add("(Title LIKE @Keyword OR Content LIKE @Keyword OR Category LIKE @Keyword OR GrowthAttribute LIKE @Keyword)");
            command.Parameters.AddWithValue("@Keyword", $"%{normalizedKeyword}%");
        }

        if (!string.IsNullOrWhiteSpace(normalizedCategory))
        {
            conditions.Add("Category = @Category");
            command.Parameters.AddWithValue("@Category", normalizedCategory);
        }

        if (status.HasValue)
        {
            conditions.Add("Status = @Status");
            command.Parameters.AddWithValue("@Status", status.Value.ToString());
        }

        if (startDueAt.HasValue)
        {
            conditions.Add("DueAt IS NOT NULL AND DueAt >= @StartDueAt");
            command.Parameters.AddWithValue("@StartDueAt", startDueAt.Value.ToString("O"));
        }

        if (inclusiveEndDueAt.HasValue)
        {
            conditions.Add("DueAt IS NOT NULL AND DueAt <= @EndDueAt");
            command.Parameters.AddWithValue("@EndDueAt", inclusiveEndDueAt.Value.ToString("O"));
        }

        command.CommandText =
            $"""
            SELECT Id, Title, Content, Category, Status, Quadrant, GrowthAttribute, Experience,
                   DueAt, CompletedAt, CompletionNote, LinkedLifeRecordId, CreatedAt, UpdatedAt
            FROM ActionTasks
            {(conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : string.Empty)}
            ORDER BY CreatedAt DESC;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            tasks.Add(Map(reader));
        }

        return tasks;
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

    private static void AddCommonParameters(SqliteCommand command, ActionTask task, int experience)
    {
        command.Parameters.AddWithValue("@Title", task.Title);
        command.Parameters.AddWithValue("@Content", task.Content);
        command.Parameters.AddWithValue("@Category", task.Category);
        command.Parameters.AddWithValue("@Status", task.Status.ToString());
        command.Parameters.AddWithValue("@Quadrant", task.Quadrant.ToString());
        command.Parameters.AddWithValue("@GrowthAttribute", task.GrowthAttribute);
        command.Parameters.AddWithValue("@Experience", experience);
        command.Parameters.AddWithValue("@DueAt", task.DueAt?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CompletedAt", task.CompletedAt?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@CompletionNote", task.CompletionNote);
        command.Parameters.AddWithValue("@LinkedLifeRecordId", task.LinkedLifeRecordId ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("@UpdatedAt", task.UpdatedAt.ToString("O"));
    }

    private static ActionTask Map(SqliteDataReader reader)
    {
        return new ActionTask
        {
            Id = reader.GetString(0),
            Title = reader.GetString(1),
            Content = reader.GetString(2),
            Category = reader.GetString(3),
            Status = TryParseStatus(reader.GetString(4)),
            Quadrant = TryParseQuadrant(reader.GetString(5)),
            GrowthAttribute = reader.GetString(6),
            Experience = reader.GetInt32(7),
            DueAt = ParseNullableDateTime(reader, 8),
            CompletedAt = ParseNullableDateTime(reader, 9),
            CompletionNote = reader.GetString(10),
            LinkedLifeRecordId = reader.IsDBNull(11) ? null : reader.GetString(11),
            CreatedAt = ParseDateTime(reader.GetString(12), DateTime.Now),
            UpdatedAt = ParseDateTime(reader.GetString(13), DateTime.Now)
        };
    }

    private static DateTime? ParseNullableDateTime(SqliteDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetString(ordinal);
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
    }

    private static DateTime ParseDateTime(string value, DateTime fallback)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : fallback;
    }

    private static ActionTaskStatus TryParseStatus(string value)
    {
        return Enum.TryParse<ActionTaskStatus>(value, ignoreCase: true, out var status)
            ? status
            : ActionTaskStatus.NotStarted;
    }

    private static ActionTaskQuadrant TryParseQuadrant(string value)
    {
        return Enum.TryParse<ActionTaskQuadrant>(value, ignoreCase: true, out var quadrant)
            ? quadrant
            : ActionTaskQuadrant.ImportantNotUrgent;
    }
}
