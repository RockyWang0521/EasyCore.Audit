using EasyCore.Audit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// MVC action filter that enriches the audit context from controller metadata.
/// </summary>
public sealed class AuditActionFilter : IAsyncActionFilter
{
    private readonly IAuditContextAccessor _contextAccessor;
    private readonly AuditOptions _options;
    private readonly AuditPayloadSerializer _parameterSerializer;
    private readonly AuditPayloadSerializer _resultSerializer;

    /// <summary>
    /// Creates the MVC audit filter.
    /// </summary>
    public AuditActionFilter(IAuditContextAccessor contextAccessor, IOptions<AuditOptions> options)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _parameterSerializer = new AuditPayloadSerializer(_options.MaxDataLength);
        _resultSerializer = new AuditPayloadSerializer(_options.MaxResponseBodyLength);
    }

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var auditContext = EnsureContext();
        if (ShouldIgnore(context))
        {
            auditContext.Ignore = true;
            await next().ConfigureAwait(false);
            return;
        }

        if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
        {
            auditContext.ModuleName ??= descriptor.ControllerName;
            auditContext.FunctionName ??= descriptor.ActionName;
        }

        ApplyAuditAttribute(context, auditContext);

        if (_options.RecordRequestParameters &&
            !auditContext.IgnoreRequestParameters &&
            context.ActionArguments.Count > 0)
        {
            auditContext.RequestParameters ??= _parameterSerializer.SerializeDictionary(
                context.ActionArguments.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase));
        }

        var executed = await next().ConfigureAwait(false);

        if (executed.Exception is not null && !executed.ExceptionHandled)
        {
            return;
        }

        if (!auditContext.IgnoreResponseBody && executed.Result is ObjectResult objectResult)
        {
            auditContext.ResponseBody ??= _resultSerializer.Serialize(objectResult.Value);
        }
    }

    private AuditContext EnsureContext()
    {
        if (_contextAccessor.Current is null)
        {
            _contextAccessor.Current = new AuditContext();
        }

        return _contextAccessor.Current;
    }

    private static bool ShouldIgnore(ActionExecutingContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.Any(m => m is IgnoreAuditAttribute))
        {
            return true;
        }

        if (context.Controller is not null)
        {
            var controllerType = context.Controller.GetType();
            if (controllerType.IsDefined(typeof(IgnoreAuditAttribute), inherit: true))
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyAuditAttribute(ActionExecutingContext context, AuditContext auditContext)
    {
        var auditAttribute = context.ActionDescriptor.EndpointMetadata
            .OfType<AuditAttribute>()
            .FirstOrDefault();

        auditAttribute ??= context.Controller?.GetType().GetCustomAttributes(typeof(AuditAttribute), inherit: true)
            .OfType<AuditAttribute>()
            .FirstOrDefault();

        if (auditAttribute is null)
        {
            auditContext.OperationType ??= OperationTypeInferrer.InferAsString(context.HttpContext.Request.Method, auditContext.FunctionName);
            return;
        }

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
            : OperationTypeInferrer.InferAsString(context.HttpContext.Request.Method, auditContext.FunctionName);
    }
}
