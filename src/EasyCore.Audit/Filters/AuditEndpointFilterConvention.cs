using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace EasyCore.Audit;

/// <summary>
/// Registers a global Minimal API endpoint filter convention so callers do not need
/// to call <c>AddEndpointFilter</c> on each endpoint.
/// </summary>
internal static class AuditEndpointFilterConvention
{
    private static readonly object Gate = new();
    private static bool _attached;

    public static void Attach(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        lock (Gate)
        {
            if (_attached)
            {
                return;
            }

            _attached = true;
        }

        var convention = CreateConvention();
        var dataSource = EnsureRouteEndpointDataSource(endpoints);
        if (dataSource is not null)
        {
            AddConvention(dataSource, convention);
        }

        // Also apply to any RouteEndpointDataSource instances already present.
        foreach (var source in endpoints.DataSources)
        {
            AddConvention(source, convention);
        }
    }

    private static Action<EndpointBuilder> CreateConvention() =>
        builder =>
        {
            if (builder.Metadata.OfType<AuditGlobalEndpointFilterMarker>().Any())
            {
                return;
            }

            builder.Metadata.Add(new AuditGlobalEndpointFilterMarker());
            builder.FilterFactories.Add((routeHandlerContext, next) =>
            {
                return invocationContext =>
                {
                    var filter = invocationContext.HttpContext.RequestServices
                        .GetRequiredService<AuditEndpointFilter>();
                    return filter.InvokeAsync(invocationContext, next);
                };
            });
        };

    private static EndpointDataSource? EnsureRouteEndpointDataSource(IEndpointRouteBuilder endpoints)
    {
        foreach (var source in endpoints.DataSources)
        {
            if (IsRouteEndpointDataSource(source))
            {
                return source;
            }
        }

        var created = TryCreateRouteEndpointDataSource(endpoints.ServiceProvider);
        if (created is not null)
        {
            endpoints.DataSources.Add(created);
        }

        return created;
    }

    private static bool IsRouteEndpointDataSource(EndpointDataSource source) =>
        source.GetType().Name == "RouteEndpointDataSource";

    private static EndpointDataSource? TryCreateRouteEndpointDataSource(IServiceProvider serviceProvider)
    {
        var type = typeof(RouteEndpoint).Assembly.GetType("Microsoft.AspNetCore.Routing.RouteEndpointDataSource");
        if (type is null)
        {
            return null;
        }

        foreach (var ctor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var parameters = ctor.GetParameters();
            try
            {
                object? instance = parameters.Length switch
                {
                    0 => ctor.Invoke(null),
                    1 when parameters[0].ParameterType.IsAssignableFrom(typeof(IServiceProvider))
                        => ctor.Invoke([serviceProvider]),
                    1 when parameters[0].ParameterType == typeof(bool)
                        => ctor.Invoke([false]),
                    2 when parameters[0].ParameterType.IsAssignableFrom(typeof(IServiceProvider))
                        => ctor.Invoke([serviceProvider, false]),
                    _ => null
                };

                if (instance is EndpointDataSource dataSource)
                {
                    return dataSource;
                }
            }
            catch
            {
                // try next constructor
            }
        }

        return null;
    }

    private static void AddConvention(EndpointDataSource dataSource, Action<EndpointBuilder> convention)
    {
        var type = dataSource.GetType();

        var conventionsProp = type.GetProperty("Conventions", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (conventionsProp?.GetValue(dataSource) is IList list)
        {
            if (!ContainsConvention(list, convention))
            {
                list.Add(convention);
            }

            return;
        }

        var field = type.GetField("_conventions", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? type.GetField("conventions", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(dataSource) is IList fieldList && !ContainsConvention(fieldList, convention))
        {
            fieldList.Add(convention);
        }
    }

    private static bool ContainsConvention(IList list, Action<EndpointBuilder> convention)
    {
        foreach (var item in list)
        {
            if (item is Action<EndpointBuilder> existing &&
                existing.Method == convention.Method &&
                ReferenceEquals(existing.Target, convention.Target))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class AuditGlobalEndpointFilterMarker;
}
