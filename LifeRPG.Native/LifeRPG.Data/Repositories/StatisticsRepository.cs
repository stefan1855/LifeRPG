using System.Globalization;
using LifeRPG.Core.Models;
using LifeRPG.Data.Services;
using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Repositories;

public sealed class StatisticsRepository
{
    public StatisticsSummary GetSummary()
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                COALESCE((SELECT COUNT(*) FROM LifeRecords), 0) AS TotalRecords,
                COALESCE((SELECT SUM(Experience) FROM LifeRecords), 0) AS TotalExperience,
                COALESCE((SELECT COUNT(*) FROM LifeRecordImages), 0) AS TotalImages,
                COALESCE((
                    SELECT COUNT(DISTINCT LifeRecordId)
                    FROM LifeRecordImages
                ), 0) AS RecordsWithImages,
                COALESCE((
                    SELECT COUNT(DISTINCT TRIM(Category))
                    FROM LifeRecords
                    WHERE Category IS NOT NULL
                      AND TRIM(Category) <> ''
                ), 0) AS CategoryCount,
                (
                    SELECT CreatedAt
                    FROM LifeRecords
                    ORDER BY CreatedAt DESC
                    LIMIT 1
                ) AS LatestRecordCreatedAt;
            """;

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return new StatisticsSummary();
        }

        var totalRecords = reader.GetInt32(0);
        var recordsWithImages = reader.GetInt32(3);

        return new StatisticsSummary
        {
            TotalRecords = totalRecords,
            TotalExperience = reader.GetInt32(1),
            TotalImages = reader.GetInt32(2),
            RecordsWithImages = recordsWithImages,
            RecordsWithoutImages = Math.Max(0, totalRecords - recordsWithImages),
            CategoryCount = reader.GetInt32(4),
            LatestRecordCreatedAt = reader.IsDBNull(5)
                ? null
                : DateTime.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }

    public List<CategoryStatistics> GetCategoryStatistics()
    {
        var results = new List<CategoryStatistics>();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                Category,
                COUNT(*) AS RecordCount,
                COALESCE(SUM(Experience), 0) AS TotalExperience,
                COALESCE(SUM(ImageCount), 0) AS ImageCount
            FROM (
                SELECT
                    CASE
                        WHEN r.Category IS NULL OR TRIM(r.Category) = '' THEN '未分类'
                        ELSE r.Category
                    END AS Category,
                    r.Experience,
                    (
                        SELECT COUNT(*)
                        FROM LifeRecordImages i
                        WHERE i.LifeRecordId = r.Id
                    ) AS ImageCount
                FROM LifeRecords r
            )
            GROUP BY Category
            ORDER BY Category COLLATE NOCASE ASC;
            """;

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new CategoryStatistics
            {
                Category = reader.GetString(0),
                RecordCount = reader.GetInt32(1),
                TotalExperience = reader.GetInt32(2),
                ImageCount = reader.GetInt32(3)
            });
        }

        return results;
    }

    public List<DailyRecordStatistics> GetRecentDailyStatistics(int days)
    {
        if (days <= 0)
        {
            days = 7;
        }

        var results = new List<DailyRecordStatistics>();
        var startDate = DateTime.Today.AddDays(-(days - 1));

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                RecordDate,
                COUNT(*) AS RecordCount,
                COALESCE(SUM(Experience), 0) AS TotalExperience,
                COALESCE(SUM(ImageCount), 0) AS ImageCount
            FROM (
                SELECT
                    date(r.CreatedAt) AS RecordDate,
                    r.Experience,
                    (
                        SELECT COUNT(*)
                        FROM LifeRecordImages i
                        WHERE i.LifeRecordId = r.Id
                    ) AS ImageCount
                FROM LifeRecords r
                WHERE r.CreatedAt >= @StartDate
            )
            GROUP BY RecordDate
            ORDER BY RecordDate ASC;
            """;
        command.Parameters.AddWithValue("@StartDate", startDate.ToString("O"));

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new DailyRecordStatistics
            {
                Date = DateTime.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                RecordCount = reader.GetInt32(1),
                TotalExperience = reader.GetInt32(2),
                ImageCount = reader.GetInt32(3)
            });
        }

        return results;
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
}
