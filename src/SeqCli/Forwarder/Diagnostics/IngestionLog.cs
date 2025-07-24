// Copyright © Datalust Pty Ltd
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
using System.Collections.Generic;
using System.Net;
using Serilog;
using Serilog.Events;

namespace SeqCli.Forwarder.Diagnostics;

static class IngestionLog
{
    const int Capacity = 100;

    static readonly InMemorySink Sink = new(Capacity);
        
    public static ILogger Log { get; }

    static IngestionLog()
    {
        Log = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Sink(Sink)
            .WriteTo.Logger(Serilog.Log.Logger)
            .CreateLogger();
    }

    public static IEnumerable<LogEvent> Read()
    {
        return Sink.Read();
    }

    public static ILogger ForClient(IPAddress? clientHostIP)
    {
        return Log.ForContext("ClientHostIP", clientHostIP);
    }

    public static ILogger ForPayload(IPAddress? clientHostIP, string payload)
    {
        var prefix = CapturePrefix(payload);
        return ForClient(clientHostIP)
            .ForContext("StartToLog", prefix.Length)
            .ForContext("DocumentStart", prefix);
    }

    static string CapturePrefix(string line)
    {
        if (line == null) throw new ArgumentNullException(nameof(line));
        var startToLog = Math.Min(line.Length, 1024);
        return line.Substring(0, startToLog);
    }
}