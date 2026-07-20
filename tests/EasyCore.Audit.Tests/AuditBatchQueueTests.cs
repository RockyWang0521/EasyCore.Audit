using EasyCore.Audit;
using EasyCore.Audit.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Tests;

public sealed class AuditBatchQueueTests
{
    [Fact]
    public async Task BackgroundService_Flushes_On_Stop()
    {
        var store = new InMemoryAuditStore();
        var options = Options.Create(new AuditOptions
        {
            EnableBatch = true,
            BatchSize = 100,
            FlushInterval = TimeSpan.FromHours(1),
            QueueCapacity = 100,
            StoreFailureMode = AuditStoreFailureMode.Ignore
        });

        var channel = new AuditLogChannel(options);
        var dispatcher = new AuditLogDispatcher([store], options, NullLogger<AuditLogDispatcher>.Instance);
        var service = new AuditBackgroundService(channel, dispatcher, options, NullLogger<AuditBackgroundService>.Instance);

        await service.StartAsync(CancellationToken.None);
        await channel.WriteAsync(new AuditLogRecord { Id = "1" }, CancellationToken.None);
        await channel.WriteAsync(new AuditLogRecord { Id = "2" }, CancellationToken.None);

        await service.StopAsync(CancellationToken.None);

        store.Records.Should().HaveCount(2);
    }
}
