using EasyCore.Audit.Models;

namespace EasyCore.Audit;

/// <summary>
/// Provides access to the current request-scoped audit context.
/// </summary>
public interface IAuditContextAccessor
{
    /// <summary>
    /// Gets or sets the current audit context.
    /// </summary>
    AuditContext? Current { get; set; }
}
