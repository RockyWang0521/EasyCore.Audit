using EasyCore.Audit.Models;
using EasyCore.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Stores.Elasticsearch;

/// <summary>
/// Query service for retrieving audit logs from Elasticsearch.
/// </summary>
public sealed class ElasticsearchAuditQueryService
{
    private readonly IElasticsearchRepository<AuditLogDocument> _repository;
    private readonly IAuditIndexNameProvider _indexNameProvider;
    private readonly AuditElasticsearchOptions _options;
    private readonly ILogger<ElasticsearchAuditQueryService> _logger;

    /// <summary>
    /// Creates the query service.
    /// </summary>
    public ElasticsearchAuditQueryService(
        IElasticsearchRepository<AuditLogDocument> repository,
        IAuditIndexNameProvider indexNameProvider,
        IOptions<AuditElasticsearchOptions> options,
        ILogger<ElasticsearchAuditQueryService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _indexNameProvider = indexNameProvider ?? throw new ArgumentNullException(nameof(indexNameProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Searches audit logs using the generic Elasticsearch search request.
    /// </summary>
    public async Task<ElasticsearchSearchResult<AuditLogRecord>> SearchAsync(
        ElasticsearchSearchRequest request,
        DateTimeOffset? indexTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_options.Enabled)
        {
            return ElasticsearchSearchResult<AuditLogRecord>.Fail("Disabled", "Elasticsearch audit store is disabled.");
        }

        var indexName = _indexNameProvider.GetIndexPattern();
        var result = await _repository.SearchAsync(request, indexName, cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogWarning("Audit search failed: {Reason}", result.ErrorReason);
            return ElasticsearchSearchResult<AuditLogRecord>.Fail(
                result.ErrorType,
                result.ErrorReason,
                result.StatusCode,
                result.Exception);
        }

        var items = result.Items.Select(AuditLogDocumentMapper.ToRecord).ToList();
        return ElasticsearchSearchResult<AuditLogRecord>.Ok(items, result.Total, result.TookMilliseconds);
    }

    /// <summary>
    /// Gets a single audit record by identifier.
    /// </summary>
    public async Task<AuditLogRecord?> GetByIdAsync(
        string id,
        DateTimeOffset? indexTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_options.Enabled)
        {
            return null;
        }

        var indexName = _indexNameProvider.GetIndexName(indexTimestamp);
        var document = await _repository.GetAsync(id, indexName, cancellationToken).ConfigureAwait(false);
        return document is null ? null : AuditLogDocumentMapper.ToRecord(document);
    }
}
