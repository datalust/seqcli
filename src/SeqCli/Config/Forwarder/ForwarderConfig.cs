namespace SeqCli.Config;

class ForwarderConfig
{
    public uint? PooledConnectionLifetimeMilliseconds { get; set; } = null;
    public ulong EventBodyLimitBytes { get; set; } = 256 * 1024;
    public ulong PayloadLimitBytes { get; set; } = 10 * 1024 * 1024;
    
    public ForwarderStorageConfig Storage { get; set; } = new();
    public ForwarderDiagnosticConfig Diagnostics { get; set; } = new();
    public ForwarderApiConfig Api { get; set; } = new();
}