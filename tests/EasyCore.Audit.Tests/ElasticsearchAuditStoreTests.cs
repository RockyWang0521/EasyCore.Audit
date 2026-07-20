using EasyCore.Audit;
using EasyCore.Audit.Models;
using EasyCore.Audit.Stores.Elasticsearch;
using EasyCore.Elasticsearch;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace EasyCore.Audit.Tests;

public sealed class ElasticsearchAuditStoreTests
{
    [Fact]
    public async Task WriteBatch_Groups_By_Index()
    {
        var repo = new Mock<IElasticsearchRepository<AuditLogDocument>>();
        var capturedIndexes = new List<string?>();

        repo.Setup(r => r.IndexManyAsync(
                It.IsAny<IEnumerable<AuditLogDocument>>(),
                It.IsAny<string?>(),
                It.IsAny<Func<AuditLogDocument, string?>?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<AuditLogDocument>, string?, Func<AuditLogDocument, string?>?, CancellationToken>(
                (_, index, _, _) => capturedIndexes.Add(index))
            .ReturnsAsync(ElasticsearchBulkResult.Ok(1));

        var indexProvider = new Mock<IAuditIndexNameProvider>();
        indexProvider.Setup(p => p.GetIndexName(It.IsAny<DateTimeOffset?>()))
            .Returns<DateTimeOffset?>(ts => ts?.Day == 18 ? "audit-2026.07.18" : "audit-2026.07.19");

        var store = new ElasticsearchAuditStore(
            repo.Object,
            indexProvider.Object,
            Options.Create(new AuditElasticsearchOptions { Enabled = true }),
            NullLogger<ElasticsearchAuditStore>.Instance);

        var records = new[]
        {
            new AuditLogRecord { Id = "1", CreatedTime = new DateTimeOffset(2026, 7, 18, 1, 0, 0, TimeSpan.Zero) },
            new AuditLogRecord { Id = "2", CreatedTime = new DateTimeOffset(2026, 7, 19, 1, 0, 0, TimeSpan.Zero) },
            new AuditLogRecord { Id = "3", CreatedTime = new DateTimeOffset(2026, 7, 18, 2, 0, 0, TimeSpan.Zero) }
        };

        await store.WriteBatchAsync(records);

        capturedIndexes.Should().BeEquivalentTo(["audit-2026.07.18", "audit-2026.07.19"]);
        repo.Verify(r => r.IndexManyAsync(
            It.IsAny<IEnumerable<AuditLogDocument>>(),
            It.IsAny<string?>(),
            It.IsAny<Func<AuditLogDocument, string?>?>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
