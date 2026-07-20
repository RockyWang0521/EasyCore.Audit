using EasyCore.Audit;
using EasyCore.Audit.Models;
using EasyCore.Audit.Stores.Elasticsearch;
using EasyCore.Elasticsearch;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Tests;

public sealed class EasyCoreAuditRegistrationTests
{
    [Fact]
    public void EasyCoreAudit_Registers_Core_Services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());
        services.AddEasyCoreAudit(o =>
        {
            o.Enabled = true;
            o.EnableBatch = false;
            o.UseFile(f => f.Directory = Path.GetTempPath());
        });

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IAuditLogWriter>().Should().BeOfType<ImmediateAuditLogWriter>();
        sp.GetRequiredService<IAuditLogDispatcher>().Should().NotBeNull();
        sp.GetRequiredService<IAuditContextAccessor>().Should().NotBeNull();
        sp.GetRequiredService<IAuditDataMasker>().Should().NotBeNull();
        sp.GetServices<IAuditStore>().Should().ContainSingle(s => s.Name == "File");
    }

    [Fact]
    public void UseElasticsearch_Registers_Elasticsearch_Store_And_Client()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());
        services.AddEasyCoreAudit(o =>
        {
            o.EnableBatch = false;
            o.UseElasticsearch(es =>
            {
                es.Nodes = ["http://localhost:9200"];
                es.IndexPrefix = "easycore-audit";
                es.UseDailyIndex = true;
                es.AutoCreateIndexTemplate = false;
                es.AutoCreateInitialIndex = false;
            });
        });

        using var sp = services.BuildServiceProvider();
        sp.GetServices<IAuditStore>().Should().ContainSingle(s => s.Name == "Elasticsearch");
        sp.GetRequiredService<IElasticsearchRepository<AuditLogDocument>>().Should().NotBeNull();
        sp.GetRequiredService<IAuditIndexNameProvider>().Should().NotBeNull();
        sp.GetRequiredService<ElasticsearchAuditQueryService>().Should().NotBeNull();
    }

    [Fact]
    public void UseDatabase_Registers_Database_Store()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());
        services.AddEasyCoreAudit(o =>
        {
            o.EnableBatch = false;
            o.UseDatabase(db =>
            {
                db.Provider = AuditDatabaseProvider.SqlServer;
                db.ConnectionString = "Server=.;Database=test;";
                db.TableName = "SysAuditLog";
            });
        });

        using var sp = services.BuildServiceProvider();
        sp.GetServices<IAuditStore>().Should().ContainSingle(s => s.Name == "Database");
    }

    [Fact]
    public void MultiStore_Registration_Works()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment());
        services.AddEasyCoreAudit(o =>
        {
            o.EnableBatch = false;
            o.UseElasticsearch(es =>
            {
                es.Nodes = ["http://localhost:9200"];
                es.AutoCreateIndexTemplate = false;
                es.AutoCreateInitialIndex = false;
            });
            o.UseFile(f => f.Directory = Path.GetTempPath());
            o.UseCustomStore<InMemoryAuditStore>();
        });

        using var sp = services.BuildServiceProvider();
        var names = sp.GetServices<IAuditStore>().Select(s => s.Name).ToList();
        names.Should().Contain(["Elasticsearch", "File", "InMemory"]);
    }

    [Fact]
    public void Duplicate_Store_Registration_Is_Ignored()
    {
        var options = new AuditOptions();
        options.UseFile(f => f.Directory = "a");
        options.UseFile(f => f.Directory = "b");
        options.StoreExtensions.Should().HaveCount(1);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

public sealed class InMemoryAuditStore : IAuditStore
{
    public string Name => "InMemory";
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
