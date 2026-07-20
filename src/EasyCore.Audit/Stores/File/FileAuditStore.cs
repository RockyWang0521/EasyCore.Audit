using System.Text.Json;
using EasyCore.Audit.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Stores.File;

/// <summary>
/// Persists audit records as JSON Lines files.
/// </summary>
public sealed class FileAuditStore : IAuditStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly AuditFileOptions _options;
    private readonly ILogger<FileAuditStore> _logger;
    private readonly Dictionary<string, SemaphoreSlim> _fileLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lockGuard = new();

    /// <summary>
    /// Store name constant.
    /// </summary>
    public const string StoreNameValue = "File";

    /// <summary>
    /// Creates the file audit store.
    /// </summary>
    public FileAuditStore(IOptions<AuditFileOptions> options, ILogger<FileAuditStore> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => StoreNameValue;

    /// <inheritdoc />
    public Task WriteAsync(AuditLogRecord record, CancellationToken cancellationToken = default)
    {
        return WriteBatchAsync([record], cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteBatchAsync(IReadOnlyList<AuditLogRecord> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);

        if (!_options.Enabled || records.Count == 0)
        {
            return;
        }

        Directory.CreateDirectory(_options.Directory);
        var filePath = GetFilePath(records[0].CreatedTime);
        var fileLock = GetFileLock(filePath);

        await fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            await using var writer = new StreamWriter(stream);
            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var json = JsonSerializer.Serialize(record, JsonOptions);
                await writer.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit records to file '{FilePath}'.", filePath);
            throw;
        }
        finally
        {
            fileLock.Release();
        }
    }

    private string GetFilePath(DateTimeOffset timestamp)
    {
        var fileName = _options.UseDailyFile
            ? $"{_options.FileNamePrefix}-{timestamp:yyyy-MM-dd}.jsonl"
            : $"{_options.FileNamePrefix}.jsonl";

        return Path.Combine(_options.Directory, fileName);
    }

    private SemaphoreSlim GetFileLock(string filePath)
    {
        lock (_lockGuard)
        {
            if (!_fileLocks.TryGetValue(filePath, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                _fileLocks[filePath] = semaphore;
            }

            return semaphore;
        }
    }
}
