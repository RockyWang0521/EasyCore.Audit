namespace EasyCore.Audit.Models;

/// <summary>
/// Mutable request-scoped audit context used for enrichment before record submission.
/// </summary>
public sealed class AuditContext
{
    /// <summary>
    /// When true, audit logging is skipped for this request.
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// When true, the audit record has already been submitted.
    /// </summary>
    public bool Submitted { get; set; }

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
    /// Operation type as a string.
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// Optional operation description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Business type label.
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// Business entity identifier.
    /// </summary>
    public string? BusinessId { get; set; }

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
    /// Department identifier.
    /// </summary>
    public string? DepartmentId { get; set; }

    /// <summary>
    /// Whether request body capture is disabled for this request.
    /// </summary>
    public bool IgnoreRequestBody { get; set; }

    /// <summary>
    /// Whether request parameter capture is disabled for this request.
    /// </summary>
    public bool IgnoreRequestParameters { get; set; }

    /// <summary>
    /// Whether response body capture is disabled for this request.
    /// </summary>
    public bool IgnoreResponseBody { get; set; }

    /// <summary>
    /// Additional custom properties.
    /// </summary>
    public Dictionary<string, object?> ExtraProperties { get; } = new(StringComparer.OrdinalIgnoreCase);
}
