using EasyCore.Audit;
using EasyCore.Audit.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Tests;

public sealed class AuditMiddlewareTests
{
    [Fact]
    public async Task Middleware_Writes_Audit_Record()
    {
        var writer = new CapturingWriter();
        var accessor = new AuditContextAccessor();
        var options = Options.Create(new AuditOptions
        {
            Enabled = true,
            ApplicationName = "TestApp",
            RecordRequestBody = false,
            RecordResponseBody = false
        });

        var middleware = new AuditMiddleware(
            async ctx =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("ok");
            },
            options,
            accessor,
            writer,
            new DefaultAuditDataMasker(options),
            new FakeEnv(),
            NullLogger<AuditMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/orders";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        writer.Records.Should().ContainSingle();
        writer.Records[0].HttpMethod.Should().Be("GET");
        writer.Records[0].RequestPath.Should().Be("/api/orders");
        writer.Records[0].Success.Should().BeTrue();
        accessor.Current.Should().BeNull();
    }

    [Fact]
    public async Task Middleware_Respects_IgnoreAudit_Via_Context()
    {
        var writer = new CapturingWriter();
        var accessor = new AuditContextAccessor();
        var options = Options.Create(new AuditOptions { Enabled = true, RecordResponseBody = false });

        var middleware = new AuditMiddleware(
            ctx =>
            {
                accessor.Current!.Ignore = true;
                return Task.CompletedTask;
            },
            options,
            accessor,
            writer,
            new DefaultAuditDataMasker(options),
            new FakeEnv(),
            NullLogger<AuditMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/x";

        await middleware.InvokeAsync(context);
        writer.Records.Should().BeEmpty();
    }

    [Fact]
    public async Task Middleware_Excludes_Swagger_Path()
    {
        var writer = new CapturingWriter();
        var accessor = new AuditContextAccessor();
        var options = Options.Create(new AuditOptions());

        var middleware = new AuditMiddleware(
            _ => Task.CompletedTask,
            options,
            accessor,
            writer,
            new DefaultAuditDataMasker(options),
            new FakeEnv(),
            NullLogger<AuditMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/swagger/index.html";

        await middleware.InvokeAsync(context);
        writer.Records.Should().BeEmpty();
    }

    [Fact]
    public async Task Middleware_Captures_Exception_Request()
    {
        var writer = new CapturingWriter();
        var accessor = new AuditContextAccessor();
        var options = Options.Create(new AuditOptions { RecordResponseBody = false });

        var middleware = new AuditMiddleware(
            _ => throw new InvalidOperationException("boom"),
            options,
            accessor,
            writer,
            new DefaultAuditDataMasker(options),
            new FakeEnv(),
            NullLogger<AuditMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/fail";

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
        writer.Records.Should().ContainSingle();
        writer.Records[0].Success.Should().BeFalse();
        writer.Records[0].ExceptionMessage.Should().Be("boom");
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

    private sealed class FakeEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
