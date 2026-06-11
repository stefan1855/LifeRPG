using Microsoft.Data.Sqlite;

namespace LifeRPG.Data.Services;

public sealed class SqliteTransactionRunner
{
    public void Run(Action<SqliteConnection, SqliteTransaction> action)
    {
        using var connection = DatabaseInitializer.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            action(connection, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
