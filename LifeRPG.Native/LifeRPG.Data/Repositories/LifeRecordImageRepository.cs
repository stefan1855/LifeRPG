using LifeRPG.Core.Models;
using LifeRPG.Data.Services;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace LifeRPG.Data.Repositories;

public sealed class LifeRecordImageRepository
{
    public void Create(LifeRecordImage image)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO LifeRecordImages (Id, LifeRecordId, FileName, FilePath, CreatedAt)
            VALUES (@Id, @LifeRecordId, @FileName, @FilePath, @CreatedAt);
            """;
        command.Parameters.AddWithValue("@Id", image.Id);
        command.Parameters.AddWithValue("@LifeRecordId", image.LifeRecordId);
        command.Parameters.AddWithValue("@FileName", image.FileName);
        command.Parameters.AddWithValue("@FilePath", image.FilePath);
        command.Parameters.AddWithValue("@CreatedAt", image.CreatedAt.ToString("O"));

        command.ExecuteNonQuery();
    }

    public List<LifeRecordImage> GetByLifeRecordId(string lifeRecordId)
    {
        var images = new List<LifeRecordImage>();

        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT Id, LifeRecordId, FileName, FilePath, CreatedAt
            FROM LifeRecordImages
            WHERE LifeRecordId = @LifeRecordId
            ORDER BY CreatedAt ASC;
            """;
        command.Parameters.AddWithValue("@LifeRecordId", lifeRecordId);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            images.Add(new LifeRecordImage
            {
                Id = reader.GetString(0),
                LifeRecordId = reader.GetString(1),
                FileName = reader.GetString(2),
                FilePath = reader.GetString(3),
                CreatedAt = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            });
        }

        return images;
    }

    public void DeleteByLifeRecordId(string lifeRecordId)
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM LifeRecordImages WHERE LifeRecordId = @LifeRecordId;";
        command.Parameters.AddWithValue("@LifeRecordId", lifeRecordId);
        command.ExecuteNonQuery();
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
