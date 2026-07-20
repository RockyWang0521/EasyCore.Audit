namespace EasyCore.Audit.Stores.Database;

/// <summary>
/// Configuration for database audit storage.
/// </summary>
public sealed class AuditDatabaseOptions
{
    /// <summary>
    /// Whether the database audit store is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Database provider.
    /// </summary>
    public AuditDatabaseProvider Provider { get; set; } = AuditDatabaseProvider.SqlServer;

    /// <summary>
    /// Connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Table name for audit records.
    /// </summary>
    public string TableName { get; set; } = "SysAuditLog";

    /// <summary>
    /// Optional schema name (SqlServer / PostgreSql).
    /// </summary>
    public string? SchemaName { get; set; }

    /// <summary>
    /// When true, creates the audit table on first write if it does not exist.
    /// </summary>
    public bool AutoCreateTable { get; set; } = true;
}
