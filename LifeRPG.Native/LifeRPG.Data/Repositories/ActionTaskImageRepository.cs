using System.Globalization;
using LifeRPG.Core.Models;
using LifeRPG.Data.Services;
using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Repositories;

public sealed class ActionTaskImageRepository
{
    public void Create(ActionTaskImage image)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO ActionTaskImages (
                Id, ActionTaskId, OriginalFileName, FileName, FilePath, RelativePath,
                FileExtension, FileSizeBytes, CreatedAt)
            VALUES (
                @Id, @ActionTaskId, @OriginalFileName, @FileName, @FilePath, @RelativePath,
                @FileExtension, @FileSizeBytes, @CreatedAt);
            """;
        AddParameters(command, image);
        command.ExecuteNonQuery();
    }

    public void Delete(string id)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ActionTaskImages WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    public void DeleteByActionTaskId(string actionTaskId)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ActionTaskImages WHERE ActionTaskId = @ActionTaskId;";
        command.Parameters.AddWithValue("@ActionTaskId", actionTaskId);
        command.ExecuteNonQuery();
    }

    public ActionTaskImage? GetById(string id)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, ActionTaskId, OriginalFileName, FileName, FilePath, RelativePath,
                   FileExtension, FileSizeBytes, CreatedAt
            FROM ActionTaskImages
            WHERE Id = @Id
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@Id", id);

        using var reader = command.ExecuteReader();
        return reader.Read() ? Map(reader) : null;
    }

    public List<ActionTaskImage> GetByActionTaskId(string actionTaskId)
    {
        var images = new List<ActionTaskImage>();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, ActionTaskId, OriginalFileName, FileName, FilePath, RelativePath,
                   FileExtension, FileSizeBytes, CreatedAt
            FROM ActionTaskImages
            WHERE ActionTaskId = @ActionTaskId
            ORDER BY CreatedAt ASC;
            """;
        command.Parameters.AddWithValue("@ActionTaskId", actionTaskId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            images.Add(Map(reader));
        }

        return images;
    }

    public int CountByActionTaskId(string actionTaskId)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM ActionTaskImages WHERE ActionTaskId = @ActionTaskId;";
        command.Parameters.AddWithValue("@ActionTaskId", actionTaskId);

        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
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

    private static void AddParameters(SqliteCommand command, ActionTaskImage image)
    {
        command.Parameters.AddWithValue("@Id", image.Id);
        command.Parameters.AddWithValue("@ActionTaskId", image.ActionTaskId);
        command.Parameters.AddWithValue("@OriginalFileName", image.OriginalFileName);
        command.Parameters.AddWithValue("@FileName", image.FileName);
        command.Parameters.AddWithValue("@FilePath", image.FilePath);
        command.Parameters.AddWithValue("@RelativePath", image.RelativePath);
        command.Parameters.AddWithValue("@FileExtension", image.FileExtension);
        command.Parameters.AddWithValue("@FileSizeBytes", image.FileSizeBytes);
        command.Parameters.AddWithValue("@CreatedAt", image.CreatedAt.ToString("O"));
    }

    private static ActionTaskImage Map(SqliteDataReader reader)
    {
        return new ActionTaskImage
        {
            Id = reader.GetString(0),
            ActionTaskId = reader.GetString(1),
            OriginalFileName = reader.GetString(2),
            FileName = reader.GetString(3),
            FilePath = reader.GetString(4),
            RelativePath = reader.GetString(5),
            FileExtension = reader.GetString(6),
            FileSizeBytes = reader.GetInt64(7),
            CreatedAt = ParseDateTime(reader.GetString(8))
        };
    }

    private static DateTime ParseDateTime(string value)
    {
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : DateTime.Now;
    }
}
