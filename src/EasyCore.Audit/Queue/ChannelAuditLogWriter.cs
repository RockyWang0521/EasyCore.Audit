using EasyCore.Audit.Models;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// Enqueues audit records to the background channel for batched dispatch.
/// </summary>
public sealed class ChannelAuditLogWriter : IAuditLogWriter
{
    private readonly AuditLogChannel _channel;
    private readonly AuditOptions _options;

    /// <summary>
    /// Creates a channel-backed writer.
    /// </summary>
    public ChannelAuditLogWriter(AuditLogChannel channel, IOptions<AuditOptions> options)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (_options.QueueFullStrategy == AuditQueueFullStrategy.Wait)
        {
            return _channel.WriteAsync(record, cancellationToken).AsTask();
        }

        return _channel.TryWriteAsync(record, cancellationToken).AsTask();
    }

    /// <inheritdoc />
    public async Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        foreach (var record in records)
        {
            await WriteAsync(record, cancellationToken).ConfigureAwait(false);
        }
    }
}
