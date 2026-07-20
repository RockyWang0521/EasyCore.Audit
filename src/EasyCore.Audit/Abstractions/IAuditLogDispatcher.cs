using EasyCore.Audit.Models;

namespace EasyCore.Audit;

/// <summary>
/// Dispatches audit log records to configured stores.
/// </summary>
public interface IAuditLogDispatcher
{
    /// <summary>
    /// Dispatches a single record to all configured stores.
    /// </summary>
    Task DispatchAsync(AuditLogRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a batch of records to all configured stores.
    /// </summary>
    Task DispatchBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default);
}
