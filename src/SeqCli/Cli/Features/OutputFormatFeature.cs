using SeqCli.Config;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

namespace SeqCli.Cli.Features
{
    class OutputFormatFeature : CommandFeature
    {
        bool _json, _noColor;

        public OutputFormatFeature(SeqCliOutputConfig outputConfig)
        {
            _noColor = outputConfig.DisableColor;
        }

        public override void Enable(OptionSet options)
        {
            options.Add(
                "json",
                "Print events in newline-delimited JSON (the default is plain text)",
                v => _json = true);

            options.Add("no-color", "Don't colorize text output", v => _noColor = true);
        }

        public Logger CreateOutputLogger()
        {
            var outputConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LevelAlias.Minimum);

            if (_json)
                outputConfiguration.WriteTo.Console(new CompactJsonFormatter());
            else
                outputConfiguration.WriteTo.Console(theme: _noColor ? ConsoleTheme.None : SystemConsoleTheme.Literate);

            return outputConfiguration.CreateLogger();
        }
    }
}
