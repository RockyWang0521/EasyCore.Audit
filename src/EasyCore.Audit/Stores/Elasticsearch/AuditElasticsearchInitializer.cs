using EasyCore.Elasticsearch;
using Elastic.Clients.Elasticsearch.Mapping;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Stores.Elasticsearch;

/// <summary>
/// Initializes Elasticsearch indexes and templates for audit documents.
/// </summary>
public interface IAuditElasticsearchInitializer
{
    /// <summary>
    /// Ensures templates and initial indexes exist.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Hosted service that creates audit index templates and initial indexes on startup.
/// </summary>
public sealed class AuditElasticsearchInitializer : IAuditElasticsearchInitializer, IHostedService
{
    private readonly IElasticsearchIndexManager _indexManager;
    private readonly IAuditIndexNameProvider _indexNameProvider;
    private readonly AuditElasticsearchOptions _options;
    private readonly ILogger<AuditElasticsearchInitializer> _logger;

    /// <summary>
    /// Creates the initializer.
    /// </summary>
    public AuditElasticsearchInitializer(
        IElasticsearchIndexManager indexManager,
        IAuditIndexNameProvider indexNameProvider,
        IOptions<AuditElasticsearchOptions> options,
        ILogger<AuditElasticsearchInitializer> logger)
    {
        _indexManager = indexManager ?? throw new ArgumentNullException(nameof(indexManager));
        _indexNameProvider = indexNameProvider ?? throw new ArgumentNullException(nameof(indexNameProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        await InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_options.AutoCreateIndexTemplate)
        {
            await CreateTemplateAsync(cancellationToken).ConfigureAwait(false);
        }

        if (_options.AutoCreateInitialIndex)
        {
            await CreateInitialIndexAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CreateTemplateAsync(CancellationToken cancellationToken)
    {
        var pattern = _indexNameProvider.GetIndexPattern();
        var result = await _indexManager.CreateOrUpdateTemplateAsync(
            _options.TemplateName,
            descriptor =>
            {
                descriptor.IndexPatterns(pattern);
                descriptor.Template(t => t
                    .Settings(s => s
                        .NumberOfShards(_options.NumberOfShards)
                        .NumberOfReplicas(_options.NumberOfReplicas))
                    .Mappings(BuildMappings()));
            },
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Failed to create audit index template '{Template}': {Reason}",
                _options.TemplateName,
                result.ErrorReason);
        }
    }

    private async Task CreateInitialIndexAsync(CancellationToken cancellationToken)
    {
        var indexName = _indexNameProvider.GetIndexName();
        if (await _indexManager.IndexExistsAsync(indexName, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var result = await _indexManager.CreateIndexAsync(
            indexName,
            _options.NumberOfShards,
            _options.NumberOfReplicas,
            BuildMappings(),
            cancellationToken).ConfigureAwait(false);

        if (!result.Success)
        {
            _logger.LogWarning(
                "Failed to create initial audit index '{Index}': {Reason}",
                indexName,
                result.ErrorReason);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_options.AliasName))
        {
            await _indexManager.CreateAliasAsync(indexName, _options.AliasName!, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static TypeMapping BuildMappings() =>
        new TypeMapping
        {
            Properties = new Properties
            {
                ["id"] = new KeywordProperty(),
                ["createdTime"] = new DateProperty(),
                ["applicationName"] = new KeywordProperty(),
                ["serviceName"] = new KeywordProperty(),
                ["environmentName"] = new KeywordProperty(),
                ["moduleName"] = new KeywordProperty(),
                ["functionName"] = new KeywordProperty(),
                ["operationName"] = new KeywordProperty(),
                ["operationType"] = new KeywordProperty(),
                ["description"] = new TextProperty { Fields = new Properties { ["keyword"] = new KeywordProperty() } },
                ["userId"] = new KeywordProperty(),
                ["userName"] = new KeywordProperty(),
                ["userDisplayName"] = new KeywordProperty(),
                ["tenantId"] = new KeywordProperty(),
                ["departmentId"] = new KeywordProperty(),
                ["clientIp"] = new KeywordProperty(),
                ["clientPort"] = new IntegerNumberProperty(),
                ["userAgent"] = new KeywordProperty { Index = false },
                ["protocol"] = new KeywordProperty(),
                ["httpMethod"] = new KeywordProperty(),
                ["requestPath"] = new KeywordProperty(),
                ["queryString"] = new KeywordProperty { Index = false },
                ["requestParameters"] = new TextProperty { Index = false },
                ["requestBody"] = new TextProperty { Index = false },
                ["responseBody"] = new TextProperty { Index = false },
                ["statusCode"] = new IntegerNumberProperty(),
                ["success"] = new BooleanProperty(),
                ["exceptionType"] = new KeywordProperty(),
                ["exceptionMessage"] = new TextProperty { Fields = new Properties { ["keyword"] = new KeywordProperty() } },
                ["exceptionStackTrace"] = new TextProperty { Index = false },
                ["elapsedMilliseconds"] = new LongNumberProperty(),
                ["traceId"] = new KeywordProperty(),
                ["spanId"] = new KeywordProperty(),
                ["requestId"] = new KeywordProperty(),
                ["businessType"] = new KeywordProperty(),
                ["businessId"] = new KeywordProperty(),
                ["beforeData"] = new TextProperty { Index = false },
                ["afterData"] = new TextProperty { Index = false },
                ["differenceData"] = new TextProperty { Index = false },
                ["extraProperties"] = new ObjectProperty { Enabled = false }
            }
        };
}
