using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Stores.File;

/// <summary>
/// Registers file audit store services.
/// </summary>
internal sealed class FileAuditStoreExtension : IAuditStoreExtension
{
    private readonly Action<AuditFileOptions> _configure;

    /// <summary>
    /// Creates the extension with a configuration delegate.
    /// </summary>
    public FileAuditStoreExtension(Action<AuditFileOptions> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <inheritdoc />
    public string StoreName => FileAuditStore.StoreNameValue;

    /// <inheritdoc />
    public void AddServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AuditFileOptions>()
            .Configure(_configure);

        services.AddSingleton<IAuditStore, FileAuditStore>();
    }
}
