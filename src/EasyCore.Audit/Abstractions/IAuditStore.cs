using EasyCore.Audit.Models;

namespace EasyCore.Audit;

/// <summary>
/// Persists audit log records to a backing store.
/// </summary>
public interface IAuditStore
{
    /// <summary>
    /// Gets the unique store name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Writes a single audit log record.
    /// </summary>
    Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a batch of audit log records.
    /// </summary>
    Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default);
}
