using EasyCore.Audit.Models;

namespace EasyCore.Audit;

/// <summary>
/// Submits audit log records for persistence (immediate or queued).
/// </summary>
public interface IAuditLogWriter
{
    /// <summary>
    /// Submits a single audit log record.
    /// </summary>
    Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a batch of audit log records.
    /// </summary>
    Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default);
}
