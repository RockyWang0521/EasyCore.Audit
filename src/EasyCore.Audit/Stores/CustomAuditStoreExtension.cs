using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.Audit.Stores;

/// <summary>
/// Registers a custom audit store implementation.
/// </summary>
/// <typeparam name="TStore">Store implementation type.</typeparam>
internal sealed class CustomAuditStoreExtension<TStore> : IAuditStoreExtension
    where TStore : class, IAuditStore
{
    /// <inheritdoc />
    public string StoreName { get; } = typeof(TStore).Name;

    /// <inheritdoc />
    public void AddServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IAuditStore, TStore>();
    }
}
