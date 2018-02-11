// Copyright 2018 Datalust Pty Ltd
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

        public static ConsoleTheme ConsoleTheme => SystemConsoleTheme.Literate;

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
                outputConfiguration.WriteTo.Console(
                    outputTemplate: "[{Timestamp:o} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                    theme: _noColor ? ConsoleTheme.None : ConsoleTheme);

            return outputConfiguration.CreateLogger();
        }
    }
}
