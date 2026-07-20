using EasyCore.Audit.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// Background service that batches queued audit records and flushes them on an interval or shutdown.
/// </summary>
public sealed class AuditBackgroundService : BackgroundService
{
    private readonly AuditLogChannel _channel;
    private readonly IAuditLogDispatcher _dispatcher;
    private readonly AuditOptions _options;
    private readonly ILogger<AuditBackgroundService> _logger;

    /// <summary>
    /// Creates the background batching service.
    /// </summary>
    public AuditBackgroundService(
        AuditLogChannel channel,
        IAuditLogDispatcher dispatcher,
        IOptions<AuditOptions> options,
        ILogger<AuditBackgroundService> logger)
    {
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<AuditLogRecord>(_options.BatchSize);
        using var timer = new PeriodicTimer(_options.FlushInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var readTask = _channel.Reader.WaitToReadAsync(stoppingToken).AsTask();
                var timerTask = timer.WaitForNextTickAsync(stoppingToken).AsTask();
                var completed = await Task.WhenAny(readTask, timerTask).ConfigureAwait(false);

                if (completed == readTask && await readTask.ConfigureAwait(false))
                {
                    while (batch.Count < _options.BatchSize && _channel.Reader.TryRead(out var record))
                    {
                        batch.Add(record);
                    }
                }

                if (batch.Count >= _options.BatchSize)
                {
                    await FlushBatchAsync(batch, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                if (completed == timerTask && batch.Count > 0)
                {
                    await FlushBatchAsync(batch, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in audit background service loop.");
            }
        }

        while (_channel.Reader.TryRead(out var remaining))
        {
            batch.Add(remaining);
        }

        if (batch.Count > 0)
        {
            await FlushBatchAsync(batch, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task FlushBatchAsync(List<AuditLogRecord> batch, CancellationToken cancellationToken)
    {
        var records = batch.ToArray();
        batch.Clear();

        try
        {
            await _dispatcher.DispatchBatchAsync(records, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush {Count} audit record(s).", records.Length);
        }
    }
}
