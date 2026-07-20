using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.Audit;

/// <summary>
/// Registers services for an audit store implementation.
/// </summary>
internal interface IAuditStoreExtension
{
    /// <summary>
    /// Gets the store name used for deduplication.
    /// </summary>
    string StoreName { get; }

    /// <summary>
    /// Adds store-specific services to the service collection.
    /// </summary>
    void AddServices(IServiceCollection services);
}
