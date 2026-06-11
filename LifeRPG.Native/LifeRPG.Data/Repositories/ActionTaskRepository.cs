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

    public ActionTask? GetByLinkedLifeRecordId(string lifeRecordId)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, Title, Content, Category, Status, Quadrant, GrowthAttribute, Experience,
                   DueAt, CompletedAt, CompletionNote, LinkedLifeRecordId, CreatedAt, UpdatedAt
            FROM ActionTasks
            WHERE LinkedLifeRecordId = @LinkedLifeRecordId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@LinkedLifeRecordId", lifeRecordId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public void LinkLifeRecord(string actionTaskId, string lifeRecordId)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE ActionTasks
            SET LinkedLifeRecordId = @LinkedLifeRecordId,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", actionTaskId);
        command.Parameters.AddWithValue("@LinkedLifeRecordId", lifeRecordId);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();
    }

    public void UnlinkLifeRecord(string actionTaskId)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE ActionTasks
            SET LinkedLifeRecordId = NULL,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", actionTaskId);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();
    }

    public void UnlinkByLifeRecordId(string lifeRecordId)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE ActionTasks
            SET LinkedLifeRecordId = NULL,
                UpdatedAt = @UpdatedAt
            WHERE LinkedLifeRecordId = @LinkedLifeRecordId;
            """;
        command.Parameters.AddWithValue("@LinkedLifeRecordId", lifeRecordId);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("O"));
        command.ExecuteNonQuery();
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

    public ActionTaskStatistics GetStatistics()
    {
        var tasks = GetAll();
        var now = DateTime.Now;
        var today = now.Date;
        var weekEndDate = today.AddDays(7);

        var activeTasks = tasks.Where(task => task.Status is not ActionTaskStatus.Completed and not ActionTaskStatus.Cancelled).ToList();
        var quadrantStatistics = Enum.GetValues<ActionTaskQuadrant>()
            .Select(quadrant =>
            {
                var quadrantTasks = tasks.Where(task => task.Quadrant == quadrant).ToList();
                return new ActionTaskQuadrantStatistic
                {
                    Quadrant = quadrant.ToString(),
                    QuadrantDisplayName = GetQuadrantDisplayName(quadrant),
                    Count = quadrantTasks.Count,
                    Experience = quadrantTasks.Sum(task => task.Experience)
                };
            })
            .ToList();

        var attributeStatistics = tasks
            .GroupBy(task => string.IsNullOrWhiteSpace(task.GrowthAttribute) ? "未设置" : task.GrowthAttribute.Trim())
            .Select(group => new ActionTaskAttributeStatistic
            {
                GrowthAttribute = group.Key,
                Count = group.Count(),
                Experience = group.Sum(task => task.Experience)
            })
            .OrderByDescending(stat => stat.Experience)
            .ThenByDescending(stat => stat.Count)
            .ThenBy(stat => stat.GrowthAttribute, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ActionTaskStatistics
        {
            TotalTasks = tasks.Count,
            NotStartedTasks = tasks.Count(task => task.Status == ActionTaskStatus.NotStarted),
            InProgressTasks = tasks.Count(task => task.Status == ActionTaskStatus.InProgress),
            CompletedTasks = tasks.Count(task => task.Status == ActionTaskStatus.Completed),
            CancelledTasks = tasks.Count(task => task.Status == ActionTaskStatus.Cancelled),
            OverdueTasks = activeTasks.Count(task => task.DueAt.HasValue && task.DueAt.Value < now),
            DueTodayTasks = activeTasks.Count(task => task.DueAt.HasValue && task.DueAt.Value.Date == today),
            DueThisWeekTasks = activeTasks.Count(task => task.DueAt.HasValue && task.DueAt.Value.Date >= today && task.DueAt.Value.Date <= weekEndDate),
            TotalExperience = tasks.Sum(task => task.Experience),
            CompletedExperience = tasks.Where(task => task.Status == ActionTaskStatus.Completed).Sum(task => task.Experience),
            QuadrantStatistics = quadrantStatistics,
            AttributeStatistics = attributeStatistics
        };
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

    private static string GetQuadrantDisplayName(ActionTaskQuadrant quadrant)
    {
        return quadrant switch
        {
            ActionTaskQuadrant.ImportantUrgent => "重要且紧急",
            ActionTaskQuadrant.ImportantNotUrgent => "重要不紧急",
            ActionTaskQuadrant.NotImportantUrgent => "不重要但紧急",
            ActionTaskQuadrant.NotImportantNotUrgent => "不重要不紧急",
            _ => quadrant.ToString()
        };
    }
}
