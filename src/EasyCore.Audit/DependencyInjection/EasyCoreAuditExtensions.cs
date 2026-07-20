using EasyCore.Audit.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// Dependency injection extensions for EasyCore audit logging.
/// </summary>
public static class EasyCoreAuditExtensions
{
    /// <summary>
    /// Adds EasyCore audit logging services.
    /// </summary>
    public static IServiceCollection EasyCoreAudit(this IServiceCollection services, Action<AuditOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AuditOptions();
        configure(options);

        foreach (var extension in options.StoreExtensions)
        {
            extension.AddServices(services);
        }

        services.AddOptions<AuditOptions>()
            .Configure(configure)
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<AuditOptions>, AuditOptionsValidator>());

        services.TryAddSingleton<IAuditContextAccessor, AuditContextAccessor>();
        services.TryAddSingleton<IAuditDataMasker, DefaultAuditDataMasker>();
        services.TryAddSingleton<IAuditLogDispatcher, AuditLogDispatcher>();
        services.TryAddScoped<AuditActionFilter>();
        services.TryAddScoped<AuditEndpointFilter>();

        if (options.EnableBatch)
        {
            services.TryAddSingleton<AuditLogChannel>();
            services.TryAddSingleton<IAuditLogWriter, ChannelAuditLogWriter>();
            if (!services.Any(d => d.ImplementationType == typeof(AuditBackgroundService)))
            {
                services.AddHostedService<AuditBackgroundService>();
            }
        }
        else
        {
            services.TryAddSingleton<IAuditLogWriter, ImmediateAuditLogWriter>();
        }

        RegisterMvcSupport(services);

        return services;
    }

    private static void RegisterMvcSupport(IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(AuditMvcRegistrationMarker)))
        {
            return;
        }

        services.AddSingleton<AuditMvcRegistrationMarker>();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IConfigureOptions<MvcOptions>, AuditMvcFilterConvention>());
    }

    private sealed class AuditMvcRegistrationMarker;
}
