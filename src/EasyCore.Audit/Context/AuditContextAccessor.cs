using EasyCore.Audit.Models;

namespace EasyCore.Audit;

/// <summary>
/// Provides request-scoped access to the current <see cref="AuditContext"/> via <see cref="AsyncLocal{T}"/>.
/// </summary>
public sealed class AuditContextAccessor : IAuditContextAccessor, IDisposable
{
    private static readonly AsyncLocal<AuditContext?> AsyncContext = new();

    /// <inheritdoc />
    public AuditContext? Current
    {
        get => AsyncContext.Value;
        set => AsyncContext.Value = value;
    }

    /// <summary>
    /// Clears the current audit context.
    /// </summary>
    public void Clear() => AsyncContext.Value = null;

    /// <inheritdoc />
    public void Dispose() => Clear();
}
