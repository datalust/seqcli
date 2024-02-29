// Copyright 2016-2017 Datalust Pty Ltd
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

using System;
using System.IO;
using Serilog.Events;

namespace Seq.Forwarder.Config
{
    public class SeqForwarderDiagnosticConfig
    {
        public string InternalLogPath { get; set; } = GetDefaultInternalLogPath();
        public LogEventLevel InternalLoggingLevel { get; set; } = LogEventLevel.Information;
        public string? InternalLogServerUri { get; set; }
        public string? InternalLogServerApiKey { get; set; }
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
                @"Seq",
                "Logs");
        }
    }
}
