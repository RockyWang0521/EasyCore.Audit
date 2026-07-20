namespace EasyCore.Audit.Stores.Elasticsearch;

/// <summary>
/// Resolves Elasticsearch index names for audit documents.
/// </summary>
public interface IAuditIndexNameProvider
{
    /// <summary>
    /// Gets the index name for the given timestamp.
    /// </summary>
    string GetIndexName(DateTimeOffset? timestamp = null);

    /// <summary>
    /// Gets the index pattern used by templates.
    /// </summary>
    string GetIndexPattern();
}
