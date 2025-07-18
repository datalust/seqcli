namespace SeqCli.Config.Forwarder;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

class SeqCliForwarderConfig
{
    public SeqCliForwarderStorageConfig Storage { get; set; } = new();
    public SeqCliForwarderDiagnosticConfig Diagnostics { get; set; } = new();
    public SeqCliForwarderApiConfig Api { get; set; } = new();
}