using System.Data.Common;
using System.Text.Json;
using EasyCore.Audit.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Npgsql;

namespace EasyCore.Audit.Stores.Database;

/// <summary>
/// Persists audit records to a relational database via ADO.NET.
/// </summary>
public sealed class DatabaseAuditStore : IAuditStore
{
    /// <summary>
    /// Store name constant.
    /// </summary>
    public const string StoreNameValue = "Database";

    private readonly AuditDatabaseOptions _options;
    private readonly ILogger<DatabaseAuditStore> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    /// <summary>
    /// Creates the database audit store.
    /// </summary>
    public DatabaseAuditStore(IOptions<AuditDatabaseOptions> options, ILogger<DatabaseAuditStore> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => StoreNameValue;

    /// <inheritdoc />
    public async Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!_options.Enabled)
        {
            return;
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = CreateInsertCommand(connection, record);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (!_options.Enabled || records.Count == 0)
        {
            return;
        }

        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            foreach (var record in records)
            {
                await using var command = CreateInsertCommand(connection, record);
                command.Transaction = transaction;
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized || !_options.AutoCreateTable)
        {
            return;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            ValidateOptions();
            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = BuildCreateTableSql();
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            _initialized = true;
            _logger.LogInformation(
                "Audit database table '{Table}' ensured for provider {Provider}.",
                GetQualifiedTableName(),
                _options.Provider);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("AuditDatabaseOptions.ConnectionString is required.");
        }

        if (_options.Provider == AuditDatabaseProvider.Custom)
        {
            throw new NotSupportedException(
                "AuditDatabaseProvider.Custom requires a custom IAuditStore. Use UseCustomStore<T>() instead.");
        }

        if (string.IsNullOrWhiteSpace(_options.TableName))
        {
            throw new InvalidOperationException("AuditDatabaseOptions.TableName is required.");
        }
    }

    private DbConnection CreateConnection()
    {
        ValidateOptions();
        return _options.Provider switch
        {
            AuditDatabaseProvider.SqlServer => new SqlConnection(_options.ConnectionString),
            AuditDatabaseProvider.MySql => new MySqlConnection(_options.ConnectionString),
            AuditDatabaseProvider.PostgreSql => new NpgsqlConnection(_options.ConnectionString),
            AuditDatabaseProvider.Sqlite => new SqliteConnection(_options.ConnectionString),
            _ => throw new NotSupportedException($"Unsupported audit database provider: {_options.Provider}.")
        };
    }

    private string GetQualifiedTableName()
    {
        var table = QuoteIdentifier(_options.TableName);
        if (string.IsNullOrWhiteSpace(_options.SchemaName))
        {
            return table;
        }

        return $"{QuoteIdentifier(_options.SchemaName)}.{table}";
    }

    private string QuoteIdentifier(string name) =>
        _options.Provider switch
        {
            AuditDatabaseProvider.MySql => $"`{name.Replace("`", "``", StringComparison.Ordinal)}`",
            AuditDatabaseProvider.PostgreSql => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"",
            AuditDatabaseProvider.Sqlite => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"",
            _ => $"[{name.Replace("]", "]]", StringComparison.Ordinal)}]"
        };

    private string Param(string name) =>
        _options.Provider == AuditDatabaseProvider.PostgreSql || _options.Provider == AuditDatabaseProvider.Sqlite
            ? $"@{name}"
            : $"@{name}";

    private string BuildCreateTableSql()
    {
        var table = GetQualifiedTableName();
        return _options.Provider switch
        {
            AuditDatabaseProvider.SqlServer => $"""
                IF OBJECT_ID(N'{EscapeSqlLiteral(GetObjectIdName())}', N'U') IS NULL
                BEGIN
                    CREATE TABLE {table} (
                        [Id] NVARCHAR(64) NOT NULL PRIMARY KEY,
                        [CreatedTime] DATETIMEOFFSET NOT NULL,
                        [ApplicationName] NVARCHAR(256) NULL,
                        [ServiceName] NVARCHAR(256) NULL,
                        [EnvironmentName] NVARCHAR(128) NULL,
                        [ModuleName] NVARCHAR(256) NULL,
                        [FunctionName] NVARCHAR(256) NULL,
                        [OperationName] NVARCHAR(256) NULL,
                        [OperationType] NVARCHAR(64) NULL,
                        [Description] NVARCHAR(MAX) NULL,
                        [UserId] NVARCHAR(128) NULL,
                        [UserName] NVARCHAR(256) NULL,
                        [UserDisplayName] NVARCHAR(256) NULL,
                        [TenantId] NVARCHAR(128) NULL,
                        [DepartmentId] NVARCHAR(128) NULL,
                        [ClientIp] NVARCHAR(64) NULL,
                        [ClientPort] INT NULL,
                        [UserAgent] NVARCHAR(1024) NULL,
                        [Protocol] NVARCHAR(32) NULL,
                        [HttpMethod] NVARCHAR(16) NULL,
                        [RequestPath] NVARCHAR(1024) NULL,
                        [QueryString] NVARCHAR(MAX) NULL,
                        [RequestParameters] NVARCHAR(MAX) NULL,
                        [RequestBody] NVARCHAR(MAX) NULL,
                        [ResponseBody] NVARCHAR(MAX) NULL,
                        [StatusCode] INT NULL,
                        [Success] BIT NOT NULL,
                        [ExceptionType] NVARCHAR(512) NULL,
                        [ExceptionMessage] NVARCHAR(MAX) NULL,
                        [ExceptionStackTrace] NVARCHAR(MAX) NULL,
                        [ElapsedMilliseconds] BIGINT NOT NULL,
                        [TraceId] NVARCHAR(128) NULL,
                        [SpanId] NVARCHAR(64) NULL,
                        [RequestId] NVARCHAR(128) NULL,
                        [BusinessType] NVARCHAR(128) NULL,
                        [BusinessId] NVARCHAR(128) NULL,
                        [BeforeData] NVARCHAR(MAX) NULL,
                        [AfterData] NVARCHAR(MAX) NULL,
                        [DifferenceData] NVARCHAR(MAX) NULL,
                        [ExtraProperties] NVARCHAR(MAX) NULL
                    );
                END
                """,
            AuditDatabaseProvider.MySql => $"""
                CREATE TABLE IF NOT EXISTS {table} (
                    `Id` VARCHAR(64) NOT NULL PRIMARY KEY,
                    `CreatedTime` DATETIME(6) NOT NULL,
                    `ApplicationName` VARCHAR(256) NULL,
                    `ServiceName` VARCHAR(256) NULL,
                    `EnvironmentName` VARCHAR(128) NULL,
                    `ModuleName` VARCHAR(256) NULL,
                    `FunctionName` VARCHAR(256) NULL,
                    `OperationName` VARCHAR(256) NULL,
                    `OperationType` VARCHAR(64) NULL,
                    `Description` LONGTEXT NULL,
                    `UserId` VARCHAR(128) NULL,
                    `UserName` VARCHAR(256) NULL,
                    `UserDisplayName` VARCHAR(256) NULL,
                    `TenantId` VARCHAR(128) NULL,
                    `DepartmentId` VARCHAR(128) NULL,
                    `ClientIp` VARCHAR(64) NULL,
                    `ClientPort` INT NULL,
                    `UserAgent` VARCHAR(1024) NULL,
                    `Protocol` VARCHAR(32) NULL,
                    `HttpMethod` VARCHAR(16) NULL,
                    `RequestPath` VARCHAR(1024) NULL,
                    `QueryString` LONGTEXT NULL,
                    `RequestParameters` LONGTEXT NULL,
                    `RequestBody` LONGTEXT NULL,
                    `ResponseBody` LONGTEXT NULL,
                    `StatusCode` INT NULL,
                    `Success` TINYINT(1) NOT NULL,
                    `ExceptionType` VARCHAR(512) NULL,
                    `ExceptionMessage` LONGTEXT NULL,
                    `ExceptionStackTrace` LONGTEXT NULL,
                    `ElapsedMilliseconds` BIGINT NOT NULL,
                    `TraceId` VARCHAR(128) NULL,
                    `SpanId` VARCHAR(64) NULL,
                    `RequestId` VARCHAR(128) NULL,
                    `BusinessType` VARCHAR(128) NULL,
                    `BusinessId` VARCHAR(128) NULL,
                    `BeforeData` LONGTEXT NULL,
                    `AfterData` LONGTEXT NULL,
                    `DifferenceData` LONGTEXT NULL,
                    `ExtraProperties` LONGTEXT NULL
                );
                """,
            AuditDatabaseProvider.PostgreSql => $"""
                CREATE TABLE IF NOT EXISTS {table} (
                    "Id" VARCHAR(64) NOT NULL PRIMARY KEY,
                    "CreatedTime" TIMESTAMPTZ NOT NULL,
                    "ApplicationName" VARCHAR(256) NULL,
                    "ServiceName" VARCHAR(256) NULL,
                    "EnvironmentName" VARCHAR(128) NULL,
                    "ModuleName" VARCHAR(256) NULL,
                    "FunctionName" VARCHAR(256) NULL,
                    "OperationName" VARCHAR(256) NULL,
                    "OperationType" VARCHAR(64) NULL,
                    "Description" TEXT NULL,
                    "UserId" VARCHAR(128) NULL,
                    "UserName" VARCHAR(256) NULL,
                    "UserDisplayName" VARCHAR(256) NULL,
                    "TenantId" VARCHAR(128) NULL,
                    "DepartmentId" VARCHAR(128) NULL,
                    "ClientIp" VARCHAR(64) NULL,
                    "ClientPort" INT NULL,
                    "UserAgent" VARCHAR(1024) NULL,
                    "Protocol" VARCHAR(32) NULL,
                    "HttpMethod" VARCHAR(16) NULL,
                    "RequestPath" VARCHAR(1024) NULL,
                    "QueryString" TEXT NULL,
                    "RequestParameters" TEXT NULL,
                    "RequestBody" TEXT NULL,
                    "ResponseBody" TEXT NULL,
                    "StatusCode" INT NULL,
                    "Success" BOOLEAN NOT NULL,
                    "ExceptionType" VARCHAR(512) NULL,
                    "ExceptionMessage" TEXT NULL,
                    "ExceptionStackTrace" TEXT NULL,
                    "ElapsedMilliseconds" BIGINT NOT NULL,
                    "TraceId" VARCHAR(128) NULL,
                    "SpanId" VARCHAR(64) NULL,
                    "RequestId" VARCHAR(128) NULL,
                    "BusinessType" VARCHAR(128) NULL,
                    "BusinessId" VARCHAR(128) NULL,
                    "BeforeData" TEXT NULL,
                    "AfterData" TEXT NULL,
                    "DifferenceData" TEXT NULL,
                    "ExtraProperties" TEXT NULL
                );
                """,
            AuditDatabaseProvider.Sqlite => $"""
                CREATE TABLE IF NOT EXISTS {table} (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "CreatedTime" TEXT NOT NULL,
                    "ApplicationName" TEXT NULL,
                    "ServiceName" TEXT NULL,
                    "EnvironmentName" TEXT NULL,
                    "ModuleName" TEXT NULL,
                    "FunctionName" TEXT NULL,
                    "OperationName" TEXT NULL,
                    "OperationType" TEXT NULL,
                    "Description" TEXT NULL,
                    "UserId" TEXT NULL,
                    "UserName" TEXT NULL,
                    "UserDisplayName" TEXT NULL,
                    "TenantId" TEXT NULL,
                    "DepartmentId" TEXT NULL,
                    "ClientIp" TEXT NULL,
                    "ClientPort" INTEGER NULL,
                    "UserAgent" TEXT NULL,
                    "Protocol" TEXT NULL,
                    "HttpMethod" TEXT NULL,
                    "RequestPath" TEXT NULL,
                    "QueryString" TEXT NULL,
                    "RequestParameters" TEXT NULL,
                    "RequestBody" TEXT NULL,
                    "ResponseBody" TEXT NULL,
                    "StatusCode" INTEGER NULL,
                    "Success" INTEGER NOT NULL,
                    "ExceptionType" TEXT NULL,
                    "ExceptionMessage" TEXT NULL,
                    "ExceptionStackTrace" TEXT NULL,
                    "ElapsedMilliseconds" INTEGER NOT NULL,
                    "TraceId" TEXT NULL,
                    "SpanId" TEXT NULL,
                    "RequestId" TEXT NULL,
                    "BusinessType" TEXT NULL,
                    "BusinessId" TEXT NULL,
                    "BeforeData" TEXT NULL,
                    "AfterData" TEXT NULL,
                    "DifferenceData" TEXT NULL,
                    "ExtraProperties" TEXT NULL
                );
                """,
            _ => throw new NotSupportedException($"Unsupported audit database provider: {_options.Provider}.")
        };
    }

    private string GetObjectIdName()
    {
        if (string.IsNullOrWhiteSpace(_options.SchemaName))
        {
            return $"dbo.{_options.TableName}";
        }

        return $"{_options.SchemaName}.{_options.TableName}";
    }

    private static string EscapeSqlLiteral(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);

    private DbCommand CreateInsertCommand(DbConnection connection, AuditLogRecord record)
    {
        var table = GetQualifiedTableName();
        var columns = string.Join(", ", ColumnNames.Select(QuoteIdentifier));
        var parameters = string.Join(", ", ColumnNames.Select(c => Param(c)));

        var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO {table} ({columns}) VALUES ({parameters})";

        AddParameter(command, "Id", record.Id);
        AddParameter(command, "CreatedTime", NormalizeDateTime(record.CreatedTime));
        AddParameter(command, "ApplicationName", record.ApplicationName);
        AddParameter(command, "ServiceName", record.ServiceName);
        AddParameter(command, "EnvironmentName", record.EnvironmentName);
        AddParameter(command, "ModuleName", record.ModuleName);
        AddParameter(command, "FunctionName", record.FunctionName);
        AddParameter(command, "OperationName", record.OperationName);
        AddParameter(command, "OperationType", record.OperationType);
        AddParameter(command, "Description", record.Description);
        AddParameter(command, "UserId", record.UserId);
        AddParameter(command, "UserName", record.UserName);
        AddParameter(command, "UserDisplayName", record.UserDisplayName);
        AddParameter(command, "TenantId", record.TenantId);
        AddParameter(command, "DepartmentId", record.DepartmentId);
        AddParameter(command, "ClientIp", record.ClientIp);
        AddParameter(command, "ClientPort", record.ClientPort);
        AddParameter(command, "UserAgent", record.UserAgent);
        AddParameter(command, "Protocol", record.Protocol);
        AddParameter(command, "HttpMethod", record.HttpMethod);
        AddParameter(command, "RequestPath", record.RequestPath);
        AddParameter(command, "QueryString", record.QueryString);
        AddParameter(command, "RequestParameters", record.RequestParameters);
        AddParameter(command, "RequestBody", record.RequestBody);
        AddParameter(command, "ResponseBody", record.ResponseBody);
        AddParameter(command, "StatusCode", record.StatusCode);
        AddParameter(command, "Success", NormalizeBoolean(record.Success));
        AddParameter(command, "ExceptionType", record.ExceptionType);
        AddParameter(command, "ExceptionMessage", record.ExceptionMessage);
        AddParameter(command, "ExceptionStackTrace", record.ExceptionStackTrace);
        AddParameter(command, "ElapsedMilliseconds", record.ElapsedMilliseconds);
        AddParameter(command, "TraceId", record.TraceId);
        AddParameter(command, "SpanId", record.SpanId);
        AddParameter(command, "RequestId", record.RequestId);
        AddParameter(command, "BusinessType", record.BusinessType);
        AddParameter(command, "BusinessId", record.BusinessId);
        AddParameter(command, "BeforeData", record.BeforeData);
        AddParameter(command, "AfterData", record.AfterData);
        AddParameter(command, "DifferenceData", record.DifferenceData);
        AddParameter(command, "ExtraProperties", SerializeExtraProperties(record.ExtraProperties));

        return command;
    }

    private object NormalizeDateTime(DateTimeOffset value) =>
        _options.Provider switch
        {
            AuditDatabaseProvider.Sqlite => value.UtcDateTime.ToString("O"),
            AuditDatabaseProvider.MySql => value.UtcDateTime,
            _ => value
        };

    private object NormalizeBoolean(bool value) =>
        _options.Provider switch
        {
            AuditDatabaseProvider.Sqlite => value ? 1 : 0,
            AuditDatabaseProvider.MySql => value ? 1 : 0,
            _ => value
        };

    private static string? SerializeExtraProperties(Dictionary<string, object?>? properties)
    {
        if (properties is null || properties.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(properties);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name.StartsWith('@') ? name : "@" + name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static readonly string[] ColumnNames =
    [
        "Id", "CreatedTime", "ApplicationName", "ServiceName", "EnvironmentName",
        "ModuleName", "FunctionName", "OperationName", "OperationType", "Description",
        "UserId", "UserName", "UserDisplayName", "TenantId", "DepartmentId",
        "ClientIp", "ClientPort", "UserAgent", "Protocol", "HttpMethod",
        "RequestPath", "QueryString", "RequestParameters", "RequestBody", "ResponseBody",
        "StatusCode", "Success", "ExceptionType", "ExceptionMessage", "ExceptionStackTrace",
        "ElapsedMilliseconds", "TraceId", "SpanId", "RequestId", "BusinessType", "BusinessId",
        "BeforeData", "AfterData", "DifferenceData", "ExtraProperties"
    ];
}
