using EasyCore.Audit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// Minimal API endpoint filter that enriches the audit context from endpoint metadata.
/// </summary>
public sealed class AuditEndpointFilter : IEndpointFilter
{
    private readonly IAuditContextAccessor _contextAccessor;
    private readonly AuditOptions _options;
    private readonly AuditPayloadSerializer _parameterSerializer;

    /// <summary>
    /// Creates the endpoint filter.
    /// </summary>
    public AuditEndpointFilter(IAuditContextAccessor contextAccessor, IOptions<AuditOptions> options)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _parameterSerializer = new AuditPayloadSerializer(_options.MaxDataLength);
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var auditContext = EnsureContext();
        var httpContext = context.HttpContext;
        var endpoint = httpContext.GetEndpoint();

        if (endpoint?.Metadata.GetMetadata<IgnoreAuditAttribute>() is not null)
        {
            auditContext.Ignore = true;
            return await next(context).ConfigureAwait(false);
        }

        var displayName = endpoint?.DisplayName;
        auditContext.FunctionName ??= displayName;
        auditContext.ModuleName ??= displayName;

        var auditAttribute = endpoint?.Metadata.GetMetadata<AuditAttribute>();
        if (auditAttribute is not null)
        {
            auditContext.OperationName ??= auditAttribute.OperationName;
            auditContext.Description ??= auditAttribute.Description;
            auditContext.ModuleName = auditAttribute.ModuleName ?? auditContext.ModuleName;
            auditContext.FunctionName = auditAttribute.FunctionName ?? auditContext.FunctionName;
            auditContext.BusinessType ??= auditAttribute.BusinessType;
            auditContext.IgnoreRequestBody = !auditAttribute.RecordRequestBody;
            auditContext.IgnoreResponseBody = !auditAttribute.RecordResponseBody;
            auditContext.IgnoreRequestParameters = !auditAttribute.RecordParameters;
            auditContext.OperationType = !string.IsNullOrWhiteSpace(auditAttribute.OperationType)
                ? auditAttribute.OperationType
                : OperationTypeInferrer.InferAsString(httpContext.Request.Method, auditContext.FunctionName);
        }
        else
        {
            auditContext.OperationType ??= OperationTypeInferrer.InferAsString(httpContext.Request.Method, auditContext.FunctionName);
        }

        if (_options.RecordRequestParameters &&
            !auditContext.IgnoreRequestParameters &&
            context.Arguments.Count > 0)
        {
            var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < context.Arguments.Count; i++)
            {
                parameters[$"arg{i}"] = context.Arguments[i];
            }

            auditContext.RequestParameters ??= _parameterSerializer.SerializeDictionary(parameters);
        }

        return await next(context).ConfigureAwait(false);
    }

    private AuditContext EnsureContext()
    {
        if (_contextAccessor.Current is null)
        {
            _contextAccessor.Current = new AuditContext();
        }

        return _contextAccessor.Current;
    }
}

/// <summary>
/// Factory for registering <see cref="AuditEndpointFilter"/> on Minimal API endpoints.
/// </summary>
public sealed class AuditEndpointFilterFactory : IEndpointFilter
{
    /// <inheritdoc />
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var filter = context.HttpContext.RequestServices.GetRequiredService<AuditEndpointFilter>();
        return filter.InvokeAsync(context, next);
    }
}
