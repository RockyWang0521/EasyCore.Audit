using EasyCore.Audit.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// Dispatches audit records to all registered stores with configurable execution and failure handling.
/// </summary>
public sealed class AuditLogDispatcher : IAuditLogDispatcher
{
    private readonly IEnumerable<IAuditStore> _stores;
    private readonly AuditOptions _options;
    private readonly ILogger<AuditLogDispatcher> _logger;

    /// <summary>
    /// Creates a dispatcher over the registered audit stores.
    /// </summary>
    public AuditLogDispatcher(
        IEnumerable<IAuditStore> stores,
        IOptions<AuditOptions> options,
        ILogger<AuditLogDispatcher> logger)
    {
        _stores = stores ?? throw new ArgumentNullException(nameof(stores));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task DispatchAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        return DispatchCoreAsync([record], cancellationToken);
    }

    /// <inheritdoc />
    public Task DispatchBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (records.Count == 0)
        {
            return Task.CompletedTask;
        }

        return DispatchCoreAsync(records, cancellationToken);
    }

    private async Task DispatchCoreAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken)
    {
        var storeList = _stores.ToList();
        if (storeList.Count == 0)
        {
            _logger.LogWarning("No audit stores are registered; {Count} record(s) will be dropped.", records.Count);
            return;
        }

        if (_options.StoreExecutionMode == AuditStoreExecutionMode.Parallel)
        {
            await DispatchParallelAsync(storeList, records, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await DispatchSequentialAsync(storeList, records, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchParallelAsync(
        IReadOnlyList<IAuditStore> stores,
        IReadOnlyList<AuditLogRecord> records,
        CancellationToken cancellationToken)
    {
        var tasks = stores.Select(store => InvokeStoreAsync(store, records, cancellationToken)).ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task DispatchSequentialAsync(
        IReadOnlyList<IAuditStore> stores,
        IReadOnlyList<AuditLogRecord> records,
        CancellationToken cancellationToken)
    {
        foreach (var store in stores)
        {
            await InvokeStoreAsync(store, records, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task InvokeStoreAsync(
        IAuditStore store,
        IReadOnlyList<AuditLogRecord> records,
        CancellationToken cancellationToken)
    {
        try
        {
            if (records.Count == 1)
            {
                await store.WriteAsync(records[0], cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await store.WriteBatchAsync(records, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            var sampleId = records[0].Id;
            _logger.LogError(
                ex,
                "Audit store '{StoreName}' failed to write audit record '{AuditId}'.",
                store.Name,
                sampleId);
            HandleStoreFailure(ex, store.Name);
        }
    }

    private void HandleStoreFailure(Exception exception, string storeName)
    {
        switch (_options.StoreFailureMode)
        {
            case AuditStoreFailureMode.Throw when !_options.IgnoreStoreExceptions:
            case AuditStoreFailureMode.Throw:
                throw new InvalidOperationException($"Audit store '{storeName}' failed.", exception);

            case AuditStoreFailureMode.StopOnFirstFailure:
                throw new InvalidOperationException($"Audit store '{storeName}' failed (StopOnFirstFailure).", exception);

            case AuditStoreFailureMode.ContinueAndAggregate:
                _logger.LogWarning("Continuing after audit store '{StoreName}' failure (ContinueAndAggregate).", storeName);
                break;

            case AuditStoreFailureMode.Ignore:
            default:
                break;
        }
    }
}
