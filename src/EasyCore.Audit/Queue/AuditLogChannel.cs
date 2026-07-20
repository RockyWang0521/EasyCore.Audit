using System.Threading.Channels;
using EasyCore.Audit.Models;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// Bounded channel for queued audit log records.
/// </summary>
public sealed class AuditLogChannel
{
    private readonly Channel<AuditLogRecord> _channel;

    /// <summary>
    /// Creates a bounded channel according to audit options.
    /// </summary>
    public AuditLogChannel(IOptions<AuditOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var auditOptions = options.Value;
        var fullMode = auditOptions.QueueFullStrategy switch
        {
            AuditQueueFullStrategy.Wait => BoundedChannelFullMode.Wait,
            AuditQueueFullStrategy.DropNewest => BoundedChannelFullMode.DropWrite,
            AuditQueueFullStrategy.DropOldest => BoundedChannelFullMode.DropOldest,
            _ => BoundedChannelFullMode.DropOldest
        };

        _channel = Channel.CreateBounded<AuditLogRecord>(new BoundedChannelOptions(auditOptions.QueueCapacity)
        {
            FullMode = fullMode,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>
    /// Gets the underlying channel reader.
    /// </summary>
    public ChannelReader<AuditLogRecord> Reader => _channel.Reader;

    /// <summary>
    /// Gets the underlying channel writer.
    /// </summary>
    public ChannelWriter<AuditLogRecord> Writer => _channel.Writer;

    /// <summary>
    /// Attempts to enqueue a record without blocking.
    /// </summary>
    public ValueTask<bool> TryWriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(_channel.Writer.TryWrite(record));
    }

    /// <summary>
    /// Enqueues a record, waiting when the channel is full and strategy is Wait.
    /// </summary>
    public ValueTask WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        return _channel.Writer.WriteAsync(record, cancellationToken);
    }
}
