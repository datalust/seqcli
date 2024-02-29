namespace SeqCli.Config;

class SeqCliForwarderConfig
{
    public uint? PooledConnectionLifetimeMilliseconds { get; set; } = null;
    public ulong EventBodyLimitBytes { get; set; } = 256 * 1024;
    public ulong PayloadLimitBytes { get; set; } = 10 * 1024 * 1024;
    
    public SeqCliStorageConfig Storage { get; set; } = new();
    public SeqCliForwarderDiagnosticConfig Diagnostics { get; set; } = new();
    public SeqCliApiConfig Api { get; set; } = new();
}