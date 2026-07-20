using EasyCore.Audit.Stores;
using EasyCore.Audit.Stores.Database;
using EasyCore.Audit.Stores.Elasticsearch;
using EasyCore.Audit.Stores.File;

namespace EasyCore.Audit;

/// <summary>
/// Configuration options for EasyCore audit logging.
/// </summary>
public sealed class AuditOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "Audit";

    /// <summary>
    /// Whether audit logging is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Application name written to audit records.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Service name written to audit records.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Environment name written to audit records.
    /// </summary>
    public string? EnvironmentName { get; set; }

    /// <summary>
    /// How multiple stores are invoked.
    /// </summary>
    public AuditStoreExecutionMode StoreExecutionMode { get; set; } = AuditStoreExecutionMode.Parallel;

    /// <summary>
    /// How store write failures are handled.
    /// </summary>
    public AuditStoreFailureMode StoreFailureMode { get; set; } = AuditStoreFailureMode.Ignore;

    /// <summary>
    /// Whether batching via background channel is enabled.
    /// </summary>
    public bool EnableBatch { get; set; } = true;

    /// <summary>
    /// Maximum batch size before flush.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Interval between batch flushes.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Maximum queued audit records.
    /// </summary>
    public int QueueCapacity { get; set; } = 10_000;

    /// <summary>
    /// Strategy when the queue is full.
    /// </summary>
    public AuditQueueFullStrategy QueueFullStrategy { get; set; } = AuditQueueFullStrategy.DropOldest;

    /// <summary>
    /// When true, store exceptions are logged but not propagated (unless failure mode is Throw).
    /// </summary>
    public bool IgnoreStoreExceptions { get; set; } = true;

    /// <summary>
    /// Maximum captured request body length in characters.
    /// </summary>
    public int MaxRequestBodyLength { get; set; } = 4096;

    /// <summary>
    /// Maximum captured response body length in characters.
    /// </summary>
    public int MaxResponseBodyLength { get; set; } = 4096;

    /// <summary>
    /// Maximum length for before/after/difference data fields.
    /// </summary>
    public int MaxDataLength { get; set; } = 8192;

    /// <summary>
    /// Whether to record action/endpoint parameters.
    /// </summary>
    public bool RecordRequestParameters { get; set; } = true;

    /// <summary>
    /// Whether to record the HTTP request body.
    /// </summary>
    public bool RecordRequestBody { get; set; } = true;

    /// <summary>
    /// Whether to record the HTTP response body.
    /// </summary>
    public bool RecordResponseBody { get; set; } = true;

    /// <summary>
    /// Whether to record exception stack traces.
    /// </summary>
    public bool RecordExceptionStackTrace { get; set; } = true;

    /// <summary>
    /// Whether to record before-data snapshots when provided via context.
    /// </summary>
    public bool RecordBeforeData { get; set; } = true;

    /// <summary>
    /// Whether to record after-data snapshots when provided via context.
    /// </summary>
    public bool RecordAfterData { get; set; } = true;

    /// <summary>
    /// Whether to calculate and record difference data when before/after are present.
    /// </summary>
    public bool CalculateDifference { get; set; }

    /// <summary>
    /// Field names (case-insensitive) whose values are masked.
    /// </summary>
    public IList<string> SensitiveFieldNames { get; set; } = new List<string>
    {
        "Password",
        "PassWord",
        "Pwd",
        "Token",
        "AccessToken",
        "RefreshToken",
        "Authorization",
        "Secret",
        "ApiKey",
        "Phone",
        "Mobile",
        "IdCard",
        "BankCard"
    };

    /// <summary>
    /// Request paths excluded from audit logging (case-insensitive prefix or segment match).
    /// </summary>
    public IList<string> ExcludedPaths { get; set; } = new List<string>
    {
        "/swagger",
        "/health",
        "/metrics",
        "/favicon.ico",
        "/openapi",
        "/prometheus",
        "/audit"
    };

    /// <summary>
    /// HTTP methods excluded from audit logging.
    /// </summary>
    public IList<string> ExcludedHttpMethods { get; set; } = new List<string>
    {
        "OPTIONS"
    };

    internal IList<IAuditStoreExtension> StoreExtensions { get; } = new List<IAuditStoreExtension>();

    internal void RegisterExtension(IAuditStoreExtension extension)
    {
        ArgumentNullException.ThrowIfNull(extension);

        if (StoreExtensions.Any(e => string.Equals(e.StoreName, extension.StoreName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        StoreExtensions.Add(extension);
    }

    /// <summary>
    /// Configures Elasticsearch as an audit store.
    /// </summary>
    public AuditOptions UseElasticsearch(Action<AuditElasticsearchOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        RegisterExtension(new ElasticsearchAuditStoreExtension(configure));
        return this;
    }

    /// <summary>
    /// Configures database as an audit store (structure reserved).
    /// </summary>
    public AuditOptions UseDatabase(Action<AuditDatabaseOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        RegisterExtension(new DatabaseAuditStoreExtension(configure));
        return this;
    }

    /// <summary>
    /// Configures file-based audit storage.
    /// </summary>
    public AuditOptions UseFile(Action<AuditFileOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        RegisterExtension(new FileAuditStoreExtension(configure));
        return this;
    }

    /// <summary>
    /// Registers a custom audit store implementation.
    /// </summary>
    public AuditOptions UseCustomStore<TStore>() where TStore : class, IAuditStore
    {
        RegisterExtension(new CustomAuditStoreExtension<TStore>());
        return this;
    }
}
