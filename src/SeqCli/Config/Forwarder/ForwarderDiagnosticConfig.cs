using System;
using System.IO;
using Serilog.Events;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SeqCli.Config.Forwarder;

public class ForwarderDiagnosticConfig
{
    public string InternalLogPath { get; set; } = GetDefaultInternalLogPath();
    public LogEventLevel InternalLoggingLevel { get; set; } = LogEventLevel.Information;
    public string? InternalLogServerUri { get; set; }
    public string? InternalLogServerApiKey { get; set; }
    public bool ExposeIngestionLog { get; set; }
    public bool IngestionLogShowDetail { get; set; }

    public static string GetDefaultInternalLogPath()
    {
        return Path.Combine(
#if WINDOWS
                // Common, here, because the service may run as Local Service, which has no obvious home
                // directory.
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
#else
            // Specific to and writable by the current user.
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
#endif
            "Seq",
            "Logs");
    }
}