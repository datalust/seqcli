// Copyright © Datalust and contributors.
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

using SeqCli.Mapping;
using Serilog.Events;

namespace SeqCli.Apps.Hosting;

/// <summary>
/// Maps level names onto Serilog's level enum for hosted Seq apps relying on the older Serilog `LogEvent`-based
/// interface (newer apps should generally use raw JSON directly).
/// </summary>
static class SerilogLevelMapping
{
    public static LogEventLevel ToSerilogLevel(string level)
    {
        if (string.IsNullOrEmpty(level))
            return LogEventLevel.Information;

        return LevelMapping.ToFullLevelName(level) switch
        {
            "Trace" or "Verbose" => LogEventLevel.Verbose,
            "Debug" => LogEventLevel.Debug,
            "Warning" => LogEventLevel.Warning,
            "Error" => LogEventLevel.Error,
            "Fatal" or "Critical" or "Emergency" or "Alert" or "Panic" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}
