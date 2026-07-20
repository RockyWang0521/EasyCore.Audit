namespace EasyCore.Audit.Models;

/// <summary>
/// Represents a persisted audit log entry.
/// </summary>
public sealed class AuditLogRecord
{
    /// <summary>
    /// Unique identifier for the audit record.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// UTC timestamp when the record was created.
    /// </summary>
    public DateTimeOffset CreatedTime { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Application name.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Service name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Environment name (e.g. Production).
    /// </summary>
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// Module or controller name.
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// Function or action name.
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Human-readable operation name.
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Operation type as a string (e.g. Create, Query).
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// Optional operation description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Authenticated user identifier.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Authenticated user name.
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Display name of the user.
    /// </summary>
    public string? UserDisplayName { get; set; }

    /// <summary>
    /// Tenant identifier.
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Department identifier.
    /// </summary>
    public string? DepartmentId { get; set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? ClientIp { get; set; }

    /// <summary>
    /// Client port.
    /// </summary>
    public int? ClientPort { get; set; }

    /// <summary>
    /// User agent string.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Request protocol (e.g. HTTP/1.1).
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// HTTP method.
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Request path.
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// Query string (without leading ?).
    /// </summary>
    public string? QueryString { get; set; }

    /// <summary>
    /// Serialized request parameters.
    /// </summary>
    public string? RequestParameters { get; set; }

    /// <summary>
    /// Request body content.
    /// </summary>
    public string? RequestBody { get; set; }

    /// <summary>
    /// Response body content.
    /// </summary>
    public string? ResponseBody { get; set; }

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Exception type name when failed.
    /// </summary>
    public string? ExceptionType { get; set; }

    /// <summary>
    /// Exception message when failed.
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// Exception stack trace when failed.
    /// </summary>
    public string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// Elapsed time in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Distributed trace identifier.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Span identifier.
    /// </summary>
    public string? SpanId { get; set; }

    /// <summary>
    /// Request correlation identifier.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Business type label.
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// Business entity identifier.
    /// </summary>
    public string? BusinessId { get; set; }

    /// <summary>
    /// State before the operation.
    /// </summary>
    public string? BeforeData { get; set; }

    /// <summary>
    /// State after the operation.
    /// </summary>
    public string? AfterData { get; set; }

    /// <summary>
    /// Diff between before and after data.
    /// </summary>
    public string? DifferenceData { get; set; }

    /// <summary>
    /// Additional custom properties.
    /// </summary>
    public Dictionary<string, object?> ExtraProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
