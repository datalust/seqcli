using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace SeqCli.Cli.Features
{
    class OutputFormatFeature : CommandFeature
    {
        bool _json;

        public override void Enable(OptionSet options)
        {
            options.Add(
                "json",
                "Print events in newline-delimited JSON (the default is plain text)",
                v => _json = true);
        }

        public Logger CreateOutputLogger()
        {
            var outputConfiguration = new LoggerConfiguration()
                .MinimumLevel.Is(LevelAlias.Minimum);

            if (_json)
                outputConfiguration.WriteTo.Console(new CompactJsonFormatter());
            else
                outputConfiguration.WriteTo.Console();

            return outputConfiguration.CreateLogger();
        }
    }
}
