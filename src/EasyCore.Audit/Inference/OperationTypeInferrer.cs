namespace EasyCore.Audit;

/// <summary>
/// Infers audit operation types from HTTP methods and method name keywords.
/// </summary>
public static class OperationTypeInferrer
{
    private static readonly string[] CreateKeywords = ["create", "add", "insert", "register", "signup", "sign-up"];
    private static readonly string[] UpdateKeywords = ["update", "edit", "modify", "patch", "set", "change"];
    private static readonly string[] DeleteKeywords = ["delete", "remove", "destroy", "cancel"];
    private static readonly string[] QueryKeywords = ["get", "list", "find", "search", "query", "fetch", "read"];
    private static readonly string[] LoginKeywords = ["login", "signin", "sign-in", "authenticate"];
    private static readonly string[] LogoutKeywords = ["logout", "signout", "sign-out"];
    private static readonly string[] ExportKeywords = ["export"];
    private static readonly string[] ImportKeywords = ["import"];
    private static readonly string[] UploadKeywords = ["upload"];
    private static readonly string[] DownloadKeywords = ["download"];
    private static readonly string[] ApproveKeywords = ["approve", "accept"];
    private static readonly string[] RejectKeywords = ["reject", "deny"];

    /// <summary>
    /// Infers an operation type from HTTP method and optional method/action name.
    /// </summary>
    public static AuditOperationType Infer(string? httpMethod, string? methodName = null)
    {
        var name = methodName?.ToLowerInvariant() ?? string.Empty;

        if (ContainsAny(name, LoginKeywords))
        {
            return AuditOperationType.Login;
        }

        if (ContainsAny(name, LogoutKeywords))
        {
            return AuditOperationType.Logout;
        }

        if (ContainsAny(name, DownloadKeywords))
        {
            return AuditOperationType.Download;
        }

        if (ContainsAny(name, UploadKeywords))
        {
            return AuditOperationType.Upload;
        }

        if (ContainsAny(name, ExportKeywords))
        {
            return AuditOperationType.Export;
        }

        if (ContainsAny(name, ImportKeywords))
        {
            return AuditOperationType.Import;
        }

        if (ContainsAny(name, ApproveKeywords))
        {
            return AuditOperationType.Approve;
        }

        if (ContainsAny(name, RejectKeywords))
        {
            return AuditOperationType.Reject;
        }

        if (ContainsAny(name, DeleteKeywords))
        {
            return AuditOperationType.Delete;
        }

        if (ContainsAny(name, CreateKeywords))
        {
            return AuditOperationType.Create;
        }

        if (ContainsAny(name, UpdateKeywords))
        {
            return AuditOperationType.Update;
        }

        if (ContainsAny(name, QueryKeywords))
        {
            return AuditOperationType.Query;
        }

        var method = httpMethod?.ToUpperInvariant();
        return method switch
        {
            "GET" or "HEAD" => AuditOperationType.Query,
            "POST" => AuditOperationType.Create,
            "PUT" or "PATCH" => AuditOperationType.Update,
            "DELETE" => AuditOperationType.Delete,
            _ => AuditOperationType.Execute
        };
    }

    /// <summary>
    /// Returns the string representation of an inferred operation type.
    /// </summary>
    public static string InferAsString(string? httpMethod, string? methodName = null) =>
        Infer(httpMethod, methodName).ToString();

    private static bool ContainsAny(string value, IEnumerable<string> keywords) =>
        keywords.Any(keyword => value.Contains(keyword, StringComparison.Ordinal));
}
