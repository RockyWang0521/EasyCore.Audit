using System.Net.Http.Json;
using EasyCore.Audit;
using EasyCore.Audit.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyCore.Audit.Tests;

public sealed class MinimalApiAuditTests
{
    [Fact]
    public async Task MinimalApi_Is_Audited_Without_Manual_AddEndpointFilter()
    {
        var writer = new CapturingWriter();
        using var host = await CreateHostAsync(writer);
        var client = host.GetTestClient();

        var response = await client.PostAsJsonAsync("/api/items", new { name = "demo", password = "secret" });
        response.EnsureSuccessStatusCode();
        await Task.Delay(80);

        writer.Records.Should().ContainSingle();
        var record = writer.Records[0];
        record.HttpMethod.Should().Be("POST");
        record.RequestPath.Should().Be("/api/items");
        record.Success.Should().BeTrue();
        record.OperationType.Should().Be("Create");
    }

    [Fact]
    public async Task MinimalApi_Respects_IgnoreAuditAttribute_Without_Manual_Filter()
    {
        var writer = new CapturingWriter();
        using var host = await CreateHostAsync(writer);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/api/health-check");
        response.EnsureSuccessStatusCode();
        await Task.Delay(80);

        writer.Records.Should().NotContain(r => r.RequestPath == "/api/health-check");
    }

    private static async Task<IHost> CreateHostAsync(CapturingWriter writer)
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.EasyCoreAudit(o =>
                    {
                        o.Enabled = true;
                        o.EnableBatch = false;
                        o.RecordRequestBody = true;
                        o.RecordResponseBody = false;
                        o.UseCustomStore<NoOpStore>();
                    });
                    services.AddSingleton<IAuditLogWriter>(writer);
                });
                webBuilder.Configure(app =>
                {
                    app.UseEasyCoreAudit();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapPost("/api/items", (ItemInput input) => Results.Ok(new { input.Name }));
                        endpoints.MapGet("/api/health-check", [IgnoreAudit] () => Results.Ok("ok"));
                    });
                });
            })
            .StartAsync();

        return host;
    }

    private sealed class CapturingWriter : IAuditLogWriter
    {
        public List<AuditLogRecord> Records { get; } = [];

        public Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default)
        {
            Records.AddRange(records);
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpStore : IAuditStore
    {
        public string Name => "NoOp";
        public Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record ItemInput(string Name, string? Password);
}
