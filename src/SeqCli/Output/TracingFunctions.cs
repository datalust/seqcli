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
using System.Globalization;
using SeqCli.Ingestion;
using Serilog.Events;

namespace SeqCli.Output;

static class TracingFunctions
{
    public static LogEventPropertyValue? Elapsed(LogEvent logEvent)
    {
        if (logEvent.Properties.TryGetValue(TraceConstants.SpanStartTimestampProperty, out var sst) &&
            sst is ScalarValue { Value: DateTime spanStart })
        {
            return new ScalarValue(logEvent.Timestamp - spanStart);
        }

        if (logEvent.Properties.TryGetValue("@st", out var st) &&
            st is ScalarValue { Value: string spanStartIso } &&
            DateTimeOffset.TryParse(spanStartIso, CultureInfo.InvariantCulture, out var spanStartDto))
        {
            return new ScalarValue(logEvent.Timestamp - spanStartDto);
        }

        return null;
    }

    public static LogEventPropertyValue? IsSpan(LogEvent logEvent)
    {
        return new ScalarValue(Elapsed(logEvent) != null);
    }
    
    public static LogEventPropertyValue? Milliseconds(LogEventPropertyValue? timeSpan)
    {
        // Truncates instead of rounding.
        if (timeSpan is ScalarValue { Value: TimeSpan ts })
            return new ScalarValue((decimal)ts.Ticks / TimeSpan.TicksPerMillisecond);

        return null;
    }
}