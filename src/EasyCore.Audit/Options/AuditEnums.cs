namespace EasyCore.Audit;

/// <summary>
/// How multiple audit stores are invoked.
/// </summary>
public enum AuditStoreExecutionMode
{
    Sequential = 0,
    Parallel = 1
}

/// <summary>
/// How store write failures are handled.
/// </summary>
public enum AuditStoreFailureMode
{
    Ignore = 0,
    Throw = 1,
    StopOnFirstFailure = 2,
    ContinueAndAggregate = 3
}

/// <summary>
/// Strategy when the audit channel is full.
/// </summary>
public enum AuditQueueFullStrategy
{
    Wait = 0,
    DropNewest = 1,
    DropOldest = 2
}

/// <summary>
/// Inferred or configured operation type.
/// </summary>
public enum AuditOperationType
{
    Query = 0,
    Create = 1,
    Update = 2,
    Delete = 3,
    Execute = 4,
    Login = 5,
    Logout = 6,
    Export = 7,
    Import = 8,
    Upload = 9,
    Download = 10,
    Approve = 11,
    Reject = 12
}

/// <summary>
/// Database provider for audit storage (structure reserved).
/// </summary>
public enum AuditDatabaseProvider
{
    SqlServer = 0,
    MySql = 1,
    PostgreSql = 2,
    Sqlite = 3,
    Custom = 4
}
