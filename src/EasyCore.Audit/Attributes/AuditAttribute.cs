namespace EasyCore.Audit;

/// <summary>
/// Marks a controller action or endpoint for audit enrichment / overrides.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AuditAttribute : Attribute
{
    /// <summary>
    /// Human-readable operation name.
    /// </summary>
    public string? OperationName { get; set; }

    /// <summary>
    /// Operation type name (e.g. Create, Query). When unset, type is inferred.
    /// </summary>
    public string? OperationType { get; set; }

    /// <summary>
    /// Optional operation description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Module name override.
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// Function name override.
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// Business type label.
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// Whether to record action parameters. Null keeps global default.
    /// </summary>
    public bool RecordParameters { get; set; } = true;

    /// <summary>
    /// Whether to record the request body. Null-like: default true; set false to skip.
    /// </summary>
    public bool RecordRequestBody { get; set; } = true;

    /// <summary>
    /// Whether to record the response body.
    /// </summary>
    public bool RecordResponseBody { get; set; } = true;

    /// <summary>
    /// Whether to record exception details for this endpoint.
    /// </summary>
    public bool RecordException { get; set; } = true;

    /// <summary>
    /// When true, request body is not captured (alias of RecordRequestBody = false).
    /// </summary>
    public bool IgnoreRequestBody
    {
        get => !RecordRequestBody;
        set => RecordRequestBody = !value;
    }

    /// <summary>
    /// When true, response body is not captured.
    /// </summary>
    public bool IgnoreResponseBody
    {
        get => !RecordResponseBody;
        set => RecordResponseBody = !value;
    }
}
