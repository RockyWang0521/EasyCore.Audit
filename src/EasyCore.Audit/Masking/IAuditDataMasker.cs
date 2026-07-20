namespace EasyCore.Audit;

/// <summary>
/// Masks sensitive data in audit payloads.
/// </summary>
public interface IAuditDataMasker
{
    /// <summary>
    /// Masks sensitive fields in the given string payload (typically JSON).
    /// </summary>
    string? Mask(string? input);

    /// <summary>
    /// Masks sensitive fields in a dictionary.
    /// </summary>
    IDictionary<string, object?>? MaskDictionary(IDictionary<string, object?>? input);
}
