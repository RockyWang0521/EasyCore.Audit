namespace EasyCore.Audit.Stores.Elasticsearch;

/// <summary>
/// Configuration for Elasticsearch audit storage.
/// </summary>
public sealed class AuditElasticsearchOptions
{
    /// <summary>
    /// Whether the Elasticsearch audit store is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Elasticsearch node URLs.
    /// </summary>
    public IList<string> Nodes { get; set; } = new List<string>();

    /// <summary>
    /// Optional basic-auth username.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Optional basic-auth password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Optional API key.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Index name prefix.
    /// </summary>
    public string IndexPrefix { get; set; } = "easycore-audit";

    /// <summary>
    /// Fixed index name. When set, daily/monthly indexing is ignored.
    /// </summary>
    public string? FixedIndexName { get; set; }

    /// <summary>
    /// When true, indexes are created per day (yyyy.MM.dd suffix).
    /// </summary>
    public bool UseDailyIndex { get; set; }

    /// <summary>
    /// When true, indexes are created per month (yyyy.MM suffix).
    /// </summary>
    public bool UseMonthlyIndex { get; set; }

    /// <summary>
    /// Index template name.
    /// </summary>
    public string TemplateName { get; set; } = "easycore-audit-template";

    /// <summary>
    /// Optional alias name for audit indexes.
    /// </summary>
    public string? AliasName { get; set; }

    /// <summary>
    /// Whether to auto-create the index template on startup.
    /// </summary>
    public bool AutoCreateIndexTemplate { get; set; } = true;

    /// <summary>
    /// Whether to auto-create the initial index on startup.
    /// </summary>
    public bool AutoCreateInitialIndex { get; set; } = true;

    /// <summary>
    /// Number of primary shards for created indexes.
    /// </summary>
    public int NumberOfShards { get; set; } = 1;

    /// <summary>
    /// Number of replica shards for created indexes.
    /// </summary>
    public int NumberOfReplicas { get; set; }

    /// <summary>
    /// Maximum transport retry count.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
}
