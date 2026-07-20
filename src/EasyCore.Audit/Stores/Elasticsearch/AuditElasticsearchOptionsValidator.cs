using Microsoft.Extensions.Options;

namespace EasyCore.Audit.Stores.Elasticsearch;

/// <summary>
/// Validates <see cref="AuditElasticsearchOptions"/>.
/// </summary>
public sealed class AuditElasticsearchOptionsValidator : IValidateOptions<AuditElasticsearchOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AuditElasticsearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (options.Nodes.Count == 0)
        {
            return ValidateOptionsResult.Fail("At least one Elasticsearch node must be configured for audit storage.");
        }

        if (options.UseDailyIndex && options.UseMonthlyIndex)
        {
            return ValidateOptionsResult.Fail("UseDailyIndex and UseMonthlyIndex cannot both be enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.IndexPrefix) && string.IsNullOrWhiteSpace(options.FixedIndexName))
        {
            return ValidateOptionsResult.Fail("IndexPrefix or FixedIndexName must be configured.");
        }

        return ValidateOptionsResult.Success;
    }
}
