namespace EasyCore.Audit.Stores.File;

/// <summary>
/// Configuration for file-based audit storage.
/// </summary>
public sealed class AuditFileOptions
{
    /// <summary>
    /// Whether the file audit store is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Directory where audit log files are written.
    /// </summary>
    public string Directory { get; set; } = "audit-logs";

    /// <summary>
    /// File name prefix.
    /// </summary>
    public string FileNamePrefix { get; set; } = "audit";

    /// <summary>
    /// When true, a new file is created per day.
    /// </summary>
    public bool UseDailyFile { get; set; } = true;
}
