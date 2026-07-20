using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Stores.Elasticsearch;

/// <summary>
/// Default index name provider supporting fixed, daily, and monthly index strategies.
/// </summary>
public sealed class DefaultAuditIndexNameProvider : IAuditIndexNameProvider
{
    private readonly AuditElasticsearchOptions _options;

    /// <summary>
    /// Creates the provider from Elasticsearch audit options.
    /// </summary>
    public DefaultAuditIndexNameProvider(IOptions<AuditElasticsearchOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string GetIndexName(DateTimeOffset? timestamp = null)
    {
        if (!string.IsNullOrWhiteSpace(_options.FixedIndexName))
        {
            return _options.FixedIndexName;
        }

        var time = timestamp ?? DateTimeOffset.UtcNow;
        var prefix = _options.IndexPrefix.TrimEnd('-', '.');

        if (_options.UseDailyIndex)
        {
            return $"{prefix}-{time:yyyy.MM.dd}";
        }

        if (_options.UseMonthlyIndex)
        {
            return $"{prefix}-{time:yyyy.MM}";
        }

        return prefix;
    }

    /// <inheritdoc />
    public string GetIndexPattern()
    {
        if (!string.IsNullOrWhiteSpace(_options.FixedIndexName))
        {
            return _options.FixedIndexName;
        }

        var prefix = _options.IndexPrefix.TrimEnd('-', '.');

        if (_options.UseDailyIndex)
        {
            return $"{prefix}-*";
        }

        if (_options.UseMonthlyIndex)
        {
            return $"{prefix}-*";
        }

        return prefix;
    }
}
