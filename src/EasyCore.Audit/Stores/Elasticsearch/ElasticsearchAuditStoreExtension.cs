using EasyCore.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Stores.Elasticsearch;

/// <summary>
/// Registers Elasticsearch audit store services.
/// </summary>
internal sealed class ElasticsearchAuditStoreExtension : IAuditStoreExtension
{
    private readonly Action<AuditElasticsearchOptions> _configure;

    /// <summary>
    /// Creates the extension with a configuration delegate.
    /// </summary>
    public ElasticsearchAuditStoreExtension(Action<AuditElasticsearchOptions> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <inheritdoc />
    public string StoreName => ElasticsearchAuditStore.StoreNameValue;

    /// <inheritdoc />
    public void AddServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var auditEsOptions = new AuditElasticsearchOptions();
        _configure(auditEsOptions);

        services.AddOptions<AuditElasticsearchOptions>()
            .Configure(_configure)
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AuditElasticsearchOptions>, AuditElasticsearchOptionsValidator>());

        services.EasyCoreElasticsearch(es =>
        {
            es.Nodes = auditEsOptions.Nodes.ToList();
            es.Username = auditEsOptions.Username;
            es.Password = auditEsOptions.Password;
            es.ApiKey = auditEsOptions.ApiKey;
            es.MaxRetryCount = auditEsOptions.MaxRetryCount;
            es.DefaultNumberOfShards = auditEsOptions.NumberOfShards;
            es.DefaultNumberOfReplicas = auditEsOptions.NumberOfReplicas;
            es.DefaultIndex = auditEsOptions.FixedIndexName ?? auditEsOptions.IndexPrefix;
        });

        services.TryAddSingleton<IAuditIndexNameProvider, DefaultAuditIndexNameProvider>();
        services.AddSingleton<IAuditStore, ElasticsearchAuditStore>();
        services.TryAddSingleton<ElasticsearchAuditQueryService>();

        if (!services.Any(d => d.ImplementationType == typeof(AuditElasticsearchInitializer)))
        {
            services.AddHostedService<AuditElasticsearchInitializer>();
        }

        services.TryAddSingleton<IAuditElasticsearchInitializer>(sp =>
            sp.GetServices<IHostedService>().OfType<AuditElasticsearchInitializer>().First());
    }
}
