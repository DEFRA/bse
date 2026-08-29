namespace BSE.Modules.BsessIntegration.Configuration;

public sealed class BsessEtlOptions
{
    public const string SectionName = "BsessEtl";

    /// <summary>
    /// Full connection string for the TSES source database.
    /// Set via environment variable <c>BsessEtl__SourceConnectionString</c>.
    /// When empty the import job exits immediately without retrying.
    /// </summary>
    public string SourceConnectionString { get; set; } = string.Empty;

    /// <summary>How often to run the full BSESS import (default: 60 minutes).</summary>
    public int ImportIntervalMinutes { get; set; } = 60;

    /// <summary>Maximum retry attempts per import cycle (default: 3).</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Base delay in seconds between retries; doubles on each attempt (default: 30).</summary>
    public int RetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Resolved enabled state — false when SourceConnectionString is absent.
    /// Override explicitly via <c>BsessEtl__Enabled</c> environment variable.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>True only when both Enabled is set and SourceConnectionString is configured.</summary>
    internal bool IsEnabled => Enabled && !string.IsNullOrWhiteSpace(SourceConnectionString);
}
