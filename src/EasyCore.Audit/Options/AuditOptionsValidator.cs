using Microsoft.Extensions.Options;

namespace EasyCore.Audit;

/// <summary>
/// Validates <see cref="AuditOptions"/> configuration.
/// </summary>
public sealed class AuditOptionsValidator : IValidateOptions<AuditOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, AuditOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.BatchSize <= 0)
        {
            return ValidateOptionsResult.Fail($"{nameof(AuditOptions.BatchSize)} must be greater than zero.");
        }

        if (options.QueueCapacity <= 0)
        {
            return ValidateOptionsResult.Fail($"{nameof(AuditOptions.QueueCapacity)} must be greater than zero.");
        }

        if (options.FlushInterval <= TimeSpan.Zero)
        {
            return ValidateOptionsResult.Fail($"{nameof(AuditOptions.FlushInterval)} must be greater than zero.");
        }

        if (options.MaxRequestBodyLength < 0)
        {
            return ValidateOptionsResult.Fail($"{nameof(AuditOptions.MaxRequestBodyLength)} cannot be negative.");
        }

        if (options.MaxResponseBodyLength < 0)
        {
            return ValidateOptionsResult.Fail($"{nameof(AuditOptions.MaxResponseBodyLength)} cannot be negative.");
        }

        if (options.MaxDataLength < 0)
        {
            return ValidateOptionsResult.Fail($"{nameof(AuditOptions.MaxDataLength)} cannot be negative.");
        }

        return ValidateOptionsResult.Success;
    }
}
