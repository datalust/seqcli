namespace SeqCli.Config.Forwarder;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

class ForwarderConfig
{
    public ForwarderStorageConfig Storage { get; set; } = new();
    public ForwarderDiagnosticConfig Diagnostics { get; set; } = new();
    public ForwarderApiConfig Api { get; set; } = new();
}