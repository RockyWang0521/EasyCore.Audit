using EasyCore.Audit.Stores.Elasticsearch;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Tests;

public sealed class AuditIndexNameProviderTests
{
    [Fact]
    public void Fixed_Index_Name()
    {
        var provider = Create(o =>
        {
            o.FixedIndexName = "easycore-audit";
            o.UseDailyIndex = false;
            o.UseMonthlyIndex = false;
        });

        provider.GetIndexName(new DateTimeOffset(2026, 7, 18, 0, 0, 0, TimeSpan.Zero))
            .Should().Be("easycore-audit");
    }

    [Fact]
    public void Daily_Index_Name()
    {
        var provider = Create(o =>
        {
            o.IndexPrefix = "easycore-audit";
            o.UseDailyIndex = true;
        });

        provider.GetIndexName(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero))
            .Should().Be("easycore-audit-2026.07.18");
    }

    [Fact]
    public void Monthly_Index_Name()
    {
        var provider = Create(o =>
        {
            o.IndexPrefix = "easycore-audit";
            o.UseMonthlyIndex = true;
        });

        provider.GetIndexName(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero))
            .Should().Be("easycore-audit-2026.07");
    }

    [Fact]
    public void Daily_And_Monthly_Conflict_Fails_Validation()
    {
        var validator = new AuditElasticsearchOptionsValidator();
        var result = validator.Validate(null, new AuditElasticsearchOptions
        {
            Nodes = ["http://localhost:9200"],
            UseDailyIndex = true,
            UseMonthlyIndex = true
        });

        result.Failed.Should().BeTrue();
    }

    private static DefaultAuditIndexNameProvider Create(Action<AuditElasticsearchOptions> configure)
    {
        var options = new AuditElasticsearchOptions();
        configure(options);
        return new DefaultAuditIndexNameProvider(Options.Create(options));
    }
}
