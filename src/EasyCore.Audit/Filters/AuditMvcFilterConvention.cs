using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// Registers <see cref="AuditActionFilter"/> globally for MVC applications.
/// </summary>
internal sealed class AuditMvcFilterConvention : IConfigureOptions<MvcOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public AuditMvcFilterConvention(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public void Configure(MvcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Filters.Any(f =>
                f is ServiceFilterAttribute attr && attr.ServiceType == typeof(AuditActionFilter)))
        {
            return;
        }

        options.Filters.Add(new ServiceFilterAttribute(typeof(AuditActionFilter))
        {
            IsReusable = true
        });
    }
}
