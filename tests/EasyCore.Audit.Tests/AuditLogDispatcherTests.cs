using EasyCore.Audit;
using EasyCore.Audit.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace EasyCore.Audit.Tests;

public sealed class AuditLogDispatcherTests
{
    [Fact]
    public async Task Parallel_Write_Invokes_All_Stores()
    {
        var store1 = new CountingStore("A");
        var store2 = new CountingStore("B");
        var dispatcher = CreateDispatcher([store1, store2], AuditStoreExecutionMode.Parallel, AuditStoreFailureMode.Ignore);

        await dispatcher.DispatchAsync(new AuditLogRecord { Id = "1" });

        store1.Count.Should().Be(1);
        store2.Count.Should().Be(1);
    }

    [Fact]
    public async Task Single_Store_Failure_Does_Not_Affect_Others_When_Ignore()
    {
        var failing = new Mock<IAuditStore>();
        failing.SetupGet(s => s.Name).Returns("Failing");
        failing.Setup(s => s.WriteAsync(It.IsAny<AuditLogRecord>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var ok = new CountingStore("Ok");
        var dispatcher = CreateDispatcher([failing.Object, ok], AuditStoreExecutionMode.Parallel, AuditStoreFailureMode.Ignore);

        await dispatcher.DispatchAsync(new AuditLogRecord { Id = "x" });

        ok.Count.Should().Be(1);
    }

    private static AuditLogDispatcher CreateDispatcher(
        IEnumerable<IAuditStore> stores,
        AuditStoreExecutionMode executionMode,
        AuditStoreFailureMode failureMode)
    {
        var options = Options.Create(new AuditOptions
        {
            StoreExecutionMode = executionMode,
            StoreFailureMode = failureMode,
            IgnoreStoreExceptions = true
        });

        return new AuditLogDispatcher(stores, options, NullLogger<AuditLogDispatcher>.Instance);
    }

    private sealed class CountingStore(string name) : IAuditStore
    {
        public string Name { get; } = name;
        public int Count { get; private set; }

        public Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
        {
            Count++;
            return Task.CompletedTask;
        }

        public Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default)
        {
            Count += records.Count;
            return Task.CompletedTask;
        }
    }
}
