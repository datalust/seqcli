using Serilog.Events;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SeqCli.Config.Forwarder;

class SeqCliForwarderDiagnosticConfig
{
    public LogEventLevel InternalLoggingLevel { get; set; } = LogEventLevel.Information;
    public string? InternalLogServerUri { get; set; }
    public string? InternalLogServerApiKey { get; set; }
    public bool ExposeIngestionLog { get; set; }
    public bool IngestionLogShowDetail { get; set; }
}
