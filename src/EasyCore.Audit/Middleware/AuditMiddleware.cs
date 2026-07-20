using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using EasyCore.Audit.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// ASP.NET Core middleware that captures HTTP audit records.
/// </summary>
public sealed class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuditOptions _options;
    private readonly IAuditContextAccessor _contextAccessor;
    private readonly IAuditLogWriter _writer;
    private readonly IAuditDataMasker _masker;
    private readonly AuditPayloadSerializer _requestSerializer;
    private readonly AuditPayloadSerializer _responseSerializer;
    private readonly AuditPayloadSerializer _dataSerializer;
    private readonly ILogger<AuditMiddleware> _logger;
    private readonly string? _environmentName;

    /// <summary>
    /// Creates the audit middleware.
    /// </summary>
    public AuditMiddleware(
        RequestDelegate next,
        IOptions<AuditOptions> options,
        IAuditContextAccessor contextAccessor,
        IAuditLogWriter writer,
        IAuditDataMasker masker,
        IHostEnvironment environment,
        ILogger<AuditMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _masker = masker ?? throw new ArgumentNullException(nameof(masker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _requestSerializer = new AuditPayloadSerializer(_options.MaxRequestBodyLength);
        _responseSerializer = new AuditPayloadSerializer(_options.MaxResponseBodyLength);
        _dataSerializer = new AuditPayloadSerializer(_options.MaxDataLength);
        _environmentName = string.IsNullOrWhiteSpace(_options.EnvironmentName)
            ? environment.EnvironmentName
            : _options.EnvironmentName;
    }

    /// <summary>
    /// Processes the HTTP request and submits an audit record when applicable.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (!_options.Enabled || ShouldExclude(httpContext))
        {
            await _next(httpContext).ConfigureAwait(false);
            return;
        }

        var context = new AuditContext();
        _contextAccessor.Current = context;

        var stopwatch = Stopwatch.StartNew();
        Exception? capturedException = null;
        Stream? originalBodyStream = null;
        MemoryStream? responseBodyStream = null;

        try
        {
            await CaptureRequestBodyAsync(httpContext, context).ConfigureAwait(false);

            if (!context.IgnoreResponseBody && _options.RecordResponseBody)
            {
                originalBodyStream = httpContext.Response.Body;
                responseBodyStream = new MemoryStream();
                httpContext.Response.Body = responseBodyStream;
            }

            try
            {
                await _next(httpContext).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                capturedException = ex;
                throw;
            }
        }
        finally
        {
            stopwatch.Stop();

            try
            {
                EnrichFromEndpoint(httpContext, context);

                if (responseBodyStream is not null && originalBodyStream is not null)
                {
                    responseBodyStream.Seek(0, SeekOrigin.Begin);
                    if (!context.Ignore)
                    {
                        using var reader = new StreamReader(responseBodyStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
                        context.ResponseBody = await reader.ReadToEndAsync().ConfigureAwait(false);
                        responseBodyStream.Seek(0, SeekOrigin.Begin);
                    }

                    await responseBodyStream.CopyToAsync(originalBodyStream).ConfigureAwait(false);
                    httpContext.Response.Body = originalBodyStream;
                }

                if (!context.Ignore && !context.Submitted)
                {
                    var record = BuildRecord(httpContext, context, stopwatch.ElapsedMilliseconds, capturedException);
                    ApplyMasking(record);
                    context.Submitted = true;
                    await _writer.WriteAsync(record, httpContext.RequestAborted).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to submit audit record for {Method} {Path}.", httpContext.Request.Method, httpContext.Request.Path);
            }
            finally
            {
                responseBodyStream?.Dispose();
                _contextAccessor.Current = null;
            }
        }
    }

    private void EnrichFromEndpoint(HttpContext httpContext, AuditContext context)
    {
        var endpoint = httpContext.GetEndpoint();
        if (endpoint is null)
        {
            CaptureRouteAndQueryParameters(httpContext, context);
            return;
        }

        if (endpoint.Metadata.GetMetadata<IgnoreAuditAttribute>() is not null)
        {
            context.Ignore = true;
            return;
        }

        var auditAttribute = endpoint.Metadata.GetMetadata<AuditAttribute>();
        if (auditAttribute is not null)
        {
            context.OperationName ??= auditAttribute.OperationName;
            context.Description ??= auditAttribute.Description;
            context.ModuleName = auditAttribute.ModuleName ?? context.ModuleName;
            context.FunctionName = auditAttribute.FunctionName ?? context.FunctionName;
            context.BusinessType ??= auditAttribute.BusinessType;
            context.IgnoreRequestBody = !auditAttribute.RecordRequestBody;
            context.IgnoreResponseBody = !auditAttribute.RecordResponseBody;
            context.IgnoreRequestParameters = !auditAttribute.RecordParameters;
            if (!string.IsNullOrWhiteSpace(auditAttribute.OperationType))
            {
                context.OperationType = auditAttribute.OperationType;
            }
        }

        context.FunctionName ??= endpoint.DisplayName;
        context.OperationType ??= OperationTypeInferrer.InferAsString(httpContext.Request.Method, context.FunctionName);
        CaptureRouteAndQueryParameters(httpContext, context);
    }

    private void CaptureRouteAndQueryParameters(HttpContext httpContext, AuditContext context)
    {
        if (!_options.RecordRequestParameters ||
            context.IgnoreRequestParameters ||
            !string.IsNullOrWhiteSpace(context.RequestParameters))
        {
            return;
        }

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in httpContext.Request.RouteValues)
        {
            if (pair.Key is "controller" or "action" or "page")
            {
                continue;
            }

            values[pair.Key] = pair.Value?.ToString();
        }

        foreach (var pair in httpContext.Request.Query)
        {
            values[pair.Key] = pair.Value.ToString();
        }

        if (values.Count > 0)
        {
            context.RequestParameters = _dataSerializer.SerializeDictionary(values);
        }
    }

    private bool ShouldExclude(HttpContext httpContext)
    {
        if (_options.ExcludedHttpMethods.Any(method =>
                string.Equals(method, httpContext.Request.Method, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var path = httpContext.Request.Path.Value ?? string.Empty;
        return _options.ExcludedPaths.Any(excluded =>
            path.StartsWith(excluded, StringComparison.OrdinalIgnoreCase) ||
            path.Contains(excluded, StringComparison.OrdinalIgnoreCase));
    }

    private async Task CaptureRequestBodyAsync(HttpContext httpContext, AuditContext context)
    {
        if (!_options.RecordRequestBody || context.IgnoreRequestBody)
        {
            return;
        }

        if (!HttpMethods.IsPost(httpContext.Request.Method) &&
            !HttpMethods.IsPut(httpContext.Request.Method) &&
            !HttpMethods.IsPatch(httpContext.Request.Method))
        {
            return;
        }

        httpContext.Request.EnableBuffering();
        httpContext.Request.Body.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        context.RequestBody = await reader.ReadToEndAsync().ConfigureAwait(false);
        httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
    }

    private AuditLogRecord BuildRecord(
        HttpContext httpContext,
        AuditContext context,
        long elapsedMilliseconds,
        Exception? exception)
    {
        var request = httpContext.Request;
        var user = httpContext.User;
        var activity = Activity.Current;

        var record = new AuditLogRecord
        {
            CreatedTime = DateTimeOffset.UtcNow,
            ApplicationName = _options.ApplicationName,
            ServiceName = _options.ServiceName,
            EnvironmentName = _environmentName,
            ModuleName = context.ModuleName,
            FunctionName = context.FunctionName,
            OperationName = context.OperationName,
            OperationType = context.OperationType ?? OperationTypeInferrer.InferAsString(request.Method, context.FunctionName),
            Description = context.Description,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"),
            UserName = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name),
            UserDisplayName = user.FindFirstValue("display_name") ?? user.FindFirstValue(ClaimTypes.GivenName),
            TenantId = GetHeader(request, "X-Tenant-Id"),
            DepartmentId = context.DepartmentId,
            ClientIp = httpContext.Connection.RemoteIpAddress?.ToString(),
            ClientPort = httpContext.Connection.RemotePort,
            UserAgent = request.Headers.UserAgent.ToString(),
            Protocol = request.Protocol,
            HttpMethod = request.Method,
            RequestPath = request.Path.Value,
            QueryString = request.QueryString.HasValue ? request.QueryString.Value?.TrimStart('?') : null,
            RequestParameters = _options.RecordRequestParameters && !context.IgnoreRequestParameters
                ? context.RequestParameters
                : null,
            RequestBody = _options.RecordRequestBody && !context.IgnoreRequestBody
                ? context.RequestBody
                : null,
            ResponseBody = _options.RecordResponseBody && !context.IgnoreResponseBody
                ? context.ResponseBody
                : null,
            StatusCode = httpContext.Response.StatusCode,
            Success = exception is null && httpContext.Response.StatusCode < 500,
            ExceptionType = exception?.GetType().FullName,
            ExceptionMessage = exception?.Message,
            ExceptionStackTrace = _options.RecordExceptionStackTrace ? exception?.StackTrace : null,
            ElapsedMilliseconds = elapsedMilliseconds,
            TraceId = GetHeader(request, "X-Trace-Id") ?? activity?.TraceId.ToString() ?? httpContext.TraceIdentifier,
            SpanId = activity?.SpanId.ToString(),
            RequestId = httpContext.TraceIdentifier,
            BusinessType = context.BusinessType,
            BusinessId = context.BusinessId,
            BeforeData = _options.RecordBeforeData ? context.BeforeData : null,
            AfterData = _options.RecordAfterData ? context.AfterData : null,
            DifferenceData = context.DifferenceData
        };

        foreach (var pair in context.ExtraProperties)
        {
            record.ExtraProperties[pair.Key] = pair.Value;
        }

        return record;
    }

    private void ApplyMasking(AuditLogRecord record)
    {
        record.RequestParameters = _masker.Mask(record.RequestParameters);
        record.RequestBody = _masker.Mask(record.RequestBody);
        record.ResponseBody = _masker.Mask(record.ResponseBody);
        record.BeforeData = _masker.Mask(record.BeforeData);
        record.AfterData = _masker.Mask(record.AfterData);
        record.DifferenceData = _masker.Mask(record.DifferenceData);

        if (record.ExtraProperties.Count > 0)
        {
            var masked = _masker.MaskDictionary(record.ExtraProperties);
            record.ExtraProperties.Clear();
            if (masked is not null)
            {
                foreach (var pair in masked)
                {
                    record.ExtraProperties[pair.Key] = pair.Value;
                }
            }
        }

        record.RequestParameters = Truncate(record.RequestParameters, _options.MaxDataLength);
        record.RequestBody = Truncate(record.RequestBody, _options.MaxRequestBodyLength);
        record.ResponseBody = Truncate(record.ResponseBody, _options.MaxResponseBodyLength);
        record.BeforeData = Truncate(record.BeforeData, _options.MaxDataLength);
        record.AfterData = Truncate(record.AfterData, _options.MaxDataLength);
        record.DifferenceData = Truncate(record.DifferenceData, _options.MaxDataLength);
    }

    private static string? GetHeader(HttpRequest request, string headerName)
    {
        return request.Headers.TryGetValue(headerName, out var values) ? values.ToString() : null;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (value is null || maxLength <= 0 || value.Length <= maxLength)
        {
            return value;
        }

        return value[..maxLength] + "...[truncated]";
    }
}
