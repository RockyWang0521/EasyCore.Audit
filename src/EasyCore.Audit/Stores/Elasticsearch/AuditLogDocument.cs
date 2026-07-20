namespace EasyCore.Audit.Stores.Elasticsearch;

/// <summary>
/// Elasticsearch document model mirroring <see cref="EasyCore.Audit.Models.AuditLogRecord"/>.
/// </summary>
public sealed class AuditLogDocument
{
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset CreatedTime { get; set; }
    public string? ApplicationName { get; set; }
    public string? ServiceName { get; set; }
    public string? EnvironmentName { get; set; }
    public string? ModuleName { get; set; }
    public string? FunctionName { get; set; }
    public string? OperationName { get; set; }
    public string? OperationType { get; set; }
    public string? Description { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserDisplayName { get; set; }
    public string? TenantId { get; set; }
    public string? DepartmentId { get; set; }
    public string? ClientIp { get; set; }
    public int? ClientPort { get; set; }
    public string? UserAgent { get; set; }
    public string? Protocol { get; set; }
    public string? HttpMethod { get; set; }
    public string? RequestPath { get; set; }
    public string? QueryString { get; set; }
    public string? RequestParameters { get; set; }
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }
    public int? StatusCode { get; set; }
    public bool Success { get; set; }
    public string? ExceptionType { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? ExceptionStackTrace { get; set; }
    public long ElapsedMilliseconds { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? RequestId { get; set; }
    public string? BusinessType { get; set; }
    public string? BusinessId { get; set; }
    public string? BeforeData { get; set; }
    public string? AfterData { get; set; }
    public string? DifferenceData { get; set; }
    public Dictionary<string, object?> ExtraProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
