using EasyCore.Audit;
using EasyCore.Audit.Models;
using EasyCore.Audit.Stores.Database;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Tests;

public sealed class DatabaseAuditStoreTests
{
    [Fact]
    public async Task WriteAsync_Persists_Record_To_Sqlite()
    {
        var connectionString = $"Data Source=file:audit-{Guid.NewGuid():N}?mode=memory&cache=shared";
        var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync();

        try
        {
            var options = Options.Create(new AuditDatabaseOptions
            {
                Provider = AuditDatabaseProvider.Sqlite,
                ConnectionString = connectionString,
                TableName = "SysAuditLog",
                AutoCreateTable = true
            });

            var store = new DatabaseAuditStore(options, NullLogger<DatabaseAuditStore>.Instance);
            var record = new AuditLogRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                ApplicationName = "Tests",
                HttpMethod = "POST",
                RequestPath = "/api/orders",
                Success = true,
                StatusCode = 200,
                ElapsedMilliseconds = 12,
                ExtraProperties = { ["k"] = "v" }
            };

            await store.WriteAsync(record);

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM SysAuditLog WHERE Id = $id";
            command.Parameters.AddWithValue("$id", record.Id);
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            count.Should().Be(1);
        }
        finally
        {
            await keepAlive.DisposeAsync();
            SqliteConnection.ClearAllPools();
        }
    }

    [Fact]
    public async Task WriteBatchAsync_Persists_Multiple_Records()
    {
        var connectionString = $"Data Source=file:audit-batch-{Guid.NewGuid():N}?mode=memory&cache=shared";
        var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync();

        try
        {
            var options = Options.Create(new AuditDatabaseOptions
            {
                Provider = AuditDatabaseProvider.Sqlite,
                ConnectionString = connectionString,
                TableName = "SysAuditLog"
            });

            var store = new DatabaseAuditStore(options, NullLogger<DatabaseAuditStore>.Instance);
            var records = new[]
            {
                new AuditLogRecord { Id = "1", Success = true },
                new AuditLogRecord { Id = "2", Success = false, StatusCode = 500 }
            };

            await store.WriteBatchAsync(records);

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM SysAuditLog";
            var count = Convert.ToInt32(await command.ExecuteScalarAsync());
            count.Should().Be(2);
        }
        finally
        {
            await keepAlive.DisposeAsync();
            SqliteConnection.ClearAllPools();
        }
    }

    [Fact]
    public void UseDatabase_Registers_Working_Store()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new FakeEnv());
        services.AddEasyCoreAudit(o =>
        {
            o.EnableBatch = false;
            o.UseDatabase(db =>
            {
                db.Provider = AuditDatabaseProvider.Sqlite;
                db.ConnectionString = "Data Source=:memory:";
                db.TableName = "SysAuditLog";
            });
        });

        using var sp = services.BuildServiceProvider();
        var store = sp.GetServices<IAuditStore>().Single(s => s.Name == "Database");
        store.Should().BeOfType<DatabaseAuditStore>();
    }

    private sealed class FakeEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
