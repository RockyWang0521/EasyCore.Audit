using EasyCore.Audit.Models;
using EasyCore.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Stores.Elasticsearch;

/// <summary>
/// Persists audit records to Elasticsearch via <see cref="IElasticsearchRepository{TDocument}"/>.
/// </summary>
public sealed class ElasticsearchAuditStore : IAuditStore
{
    private readonly IElasticsearchRepository<AuditLogDocument> _repository;
    private readonly IAuditIndexNameProvider _indexNameProvider;
    private readonly AuditElasticsearchOptions _options;
    private readonly ILogger<ElasticsearchAuditStore> _logger;

    /// <summary>
    /// Store name constant.
    /// </summary>
    public const string StoreNameValue = "Elasticsearch";

    /// <summary>
    /// Creates the Elasticsearch audit store.
    /// </summary>
    public ElasticsearchAuditStore(
        IElasticsearchRepository<AuditLogDocument> repository,
        IAuditIndexNameProvider indexNameProvider,
        IOptions<AuditElasticsearchOptions> options,
        ILogger<ElasticsearchAuditStore> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _indexNameProvider = indexNameProvider ?? throw new ArgumentNullException(nameof(indexNameProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => StoreNameValue;

    /// <inheritdoc />
    public async Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!_options.Enabled)
        {
            return;
        }

        var document = AuditLogDocumentMapper.ToDocument(record);
        var indexName = _indexNameProvider.GetIndexName(record.CreatedTime);
        var result = await _repository.IndexAsync(document, indexName, document.Id, cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Failed to index audit record {Id} to '{Index}': {Reason}",
                record.Id,
                indexName,
                result.ErrorReason);
            throw new InvalidOperationException($"Failed to write audit record to Elasticsearch: {result.ErrorReason}");
        }
    }

    /// <inheritdoc />
    public async Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (!_options.Enabled || records.Count == 0)
        {
            return;
        }

        var groups = records
            .Select(AuditLogDocumentMapper.ToDocument)
            .GroupBy(doc => _indexNameProvider.GetIndexName(doc.CreatedTime));

        foreach (var group in groups)
        {
            var result = await _repository.IndexManyAsync(
                group,
                group.Key,
                doc => doc.Id,
                cancellationToken).ConfigureAwait(false);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Failed to bulk index {Count} audit record(s) to '{Index}': {Reason}",
                    group.Count(),
                    group.Key,
                    result.ErrorReason);
                throw new InvalidOperationException($"Failed to bulk write audit records to Elasticsearch: {result.ErrorReason}");
            }
        }
    }
}

/// <summary>
/// Maps between audit records and Elasticsearch documents.
/// </summary>
internal static class AuditLogDocumentMapper
{
    public static AuditLogDocument ToDocument(AuditLogRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new AuditLogDocument
        {
            Id = record.Id,
            CreatedTime = record.CreatedTime,
            ApplicationName = record.ApplicationName,
            ServiceName = record.ServiceName,
            EnvironmentName = record.EnvironmentName,
            ModuleName = record.ModuleName,
            FunctionName = record.FunctionName,
            OperationName = record.OperationName,
            OperationType = record.OperationType,
            Description = record.Description,
            UserId = record.UserId,
            UserName = record.UserName,
            UserDisplayName = record.UserDisplayName,
            TenantId = record.TenantId,
            DepartmentId = record.DepartmentId,
            ClientIp = record.ClientIp,
            ClientPort = record.ClientPort,
            UserAgent = record.UserAgent,
            Protocol = record.Protocol,
            HttpMethod = record.HttpMethod,
            RequestPath = record.RequestPath,
            QueryString = record.QueryString,
            RequestParameters = record.RequestParameters,
            RequestBody = record.RequestBody,
            ResponseBody = record.ResponseBody,
            StatusCode = record.StatusCode,
            Success = record.Success,
            ExceptionType = record.ExceptionType,
            ExceptionMessage = record.ExceptionMessage,
            ExceptionStackTrace = record.ExceptionStackTrace,
            ElapsedMilliseconds = record.ElapsedMilliseconds,
            TraceId = record.TraceId,
            SpanId = record.SpanId,
            RequestId = record.RequestId,
            BusinessType = record.BusinessType,
            BusinessId = record.BusinessId,
            BeforeData = record.BeforeData,
            AfterData = record.AfterData,
            DifferenceData = record.DifferenceData,
            ExtraProperties = new Dictionary<string, object?>(record.ExtraProperties, StringComparer.OrdinalIgnoreCase)
        };
    }

    public static AuditLogRecord ToRecord(AuditLogDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new AuditLogRecord
        {
            Id = document.Id,
            CreatedTime = document.CreatedTime,
            ApplicationName = document.ApplicationName,
            ServiceName = document.ServiceName,
            EnvironmentName = document.EnvironmentName,
            ModuleName = document.ModuleName,
            FunctionName = document.FunctionName,
            OperationName = document.OperationName,
            OperationType = document.OperationType,
            Description = document.Description,
            UserId = document.UserId,
            UserName = document.UserName,
            UserDisplayName = document.UserDisplayName,
            TenantId = document.TenantId,
            DepartmentId = document.DepartmentId,
            ClientIp = document.ClientIp,
            ClientPort = document.ClientPort,
            UserAgent = document.UserAgent,
            Protocol = document.Protocol,
            HttpMethod = document.HttpMethod,
            RequestPath = document.RequestPath,
            QueryString = document.QueryString,
            RequestParameters = document.RequestParameters,
            RequestBody = document.RequestBody,
            ResponseBody = document.ResponseBody,
            StatusCode = document.StatusCode,
            Success = document.Success,
            ExceptionType = document.ExceptionType,
            ExceptionMessage = document.ExceptionMessage,
            ExceptionStackTrace = document.ExceptionStackTrace,
            ElapsedMilliseconds = document.ElapsedMilliseconds,
            TraceId = document.TraceId,
            SpanId = document.SpanId,
            RequestId = document.RequestId,
            BusinessType = document.BusinessType,
            BusinessId = document.BusinessId,
            BeforeData = document.BeforeData,
            AfterData = document.AfterData,
            DifferenceData = document.DifferenceData,
            ExtraProperties = new Dictionary<string, object?>(document.ExtraProperties, StringComparer.OrdinalIgnoreCase)
        };
    }
}
