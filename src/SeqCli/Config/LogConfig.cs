using Serilog.Core;
using Serilog.Events;

namespace SeqCli.Config
{
    static class LogConfig
    {
        internal static readonly LoggingLevelSwitch SharedLevelSwitch = new LoggingLevelSwitch(LogEventLevel.Error);
    }
}