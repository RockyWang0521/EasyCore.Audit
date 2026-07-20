namespace EasyCore.Audit;

/// <summary>
/// Excludes a controller action or endpoint from audit logging.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class IgnoreAuditAttribute : Attribute;
