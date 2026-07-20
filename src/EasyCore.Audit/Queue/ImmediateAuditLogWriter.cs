using EasyCore.Audit.Models;

namespace EasyCore.Audit;

/// <summary>
/// Writes audit records immediately through the dispatcher.
/// </summary>
public sealed class ImmediateAuditLogWriter : IAuditLogWriter
{
    private readonly IAuditLogDispatcher _dispatcher;

    /// <summary>
    /// Creates an immediate writer.
    /// </summary>
    public ImmediateAuditLogWriter(IAuditLogDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <inheritdoc />
    public Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        return _dispatcher.DispatchAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        return _dispatcher.DispatchBatchAsync(records, cancellationToken);
    }
}
