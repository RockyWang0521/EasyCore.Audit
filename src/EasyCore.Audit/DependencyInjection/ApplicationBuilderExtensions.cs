using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace EasyCore.Audit;

/// <summary>
/// Application builder extensions for EasyCore audit middleware.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds audit logging middleware to the pipeline.
    /// </summary>
    public static IApplicationBuilder UseEasyCoreAudit(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (app is IEndpointRouteBuilder endpointRouteBuilder)
        {
            AuditEndpointFilterConvention.Attach(endpointRouteBuilder);
        }

        return app.UseMiddleware<AuditMiddleware>();
    }

    /// <summary>
    /// Adds audit logging middleware and globally attaches the Minimal API endpoint filter.
    /// </summary>
    public static WebApplication UseEasyCoreAudit(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        AuditEndpointFilterConvention.Attach(app);
        app.UseMiddleware<AuditMiddleware>();
        return app;
    }
}
