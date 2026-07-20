using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Stores.Database;

/// <summary>
/// Registers database audit store services.
/// </summary>
internal sealed class DatabaseAuditStoreExtension : IAuditStoreExtension
{
    private readonly Action<AuditDatabaseOptions> _configure;

    /// <summary>
    /// Creates the extension with a configuration delegate.
    /// </summary>
    public DatabaseAuditStoreExtension(Action<AuditDatabaseOptions> configure)
    {
        _configure = configure ?? throw new ArgumentNullException(nameof(configure));
    }

    /// <inheritdoc />
    public string StoreName => DatabaseAuditStore.StoreNameValue;

    /// <inheritdoc />
    public void AddServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AuditDatabaseOptions>()
            .Configure(_configure)
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "AuditDatabaseOptions.ConnectionString is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.TableName), "AuditDatabaseOptions.TableName is required.")
            .Validate(o => o.Provider != AuditDatabaseProvider.Custom, "Use UseCustomStore<T>() for custom database providers.")
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AuditDatabaseOptions>, AuditDatabaseOptionsValidator>());

        services.AddSingleton<IAuditStore, DatabaseAuditStore>();
    }
}

/// <summary>
/// Validates <see cref="AuditDatabaseOptions"/>.
/// </summary>
public sealed class AuditDatabaseOptionsValidator : IValidateOptions<AuditDatabaseOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AuditDatabaseOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            failures.Add("AuditDatabaseOptions.ConnectionString is required.");
        }

        if (string.IsNullOrWhiteSpace(options.TableName))
        {
            failures.Add("AuditDatabaseOptions.TableName is required.");
        }

        if (options.Provider == AuditDatabaseProvider.Custom)
        {
            failures.Add("AuditDatabaseProvider.Custom is not supported by DatabaseAuditStore. Use UseCustomStore<T>().");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
