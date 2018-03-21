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

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeqCli.PlainText.Framing;
using Serilog.Formatting.Compact.Reader;
using Superpower;
using Superpower.Model;

namespace SeqCli.Ingestion
{
    class JsonLogEventReader : ILogEventReader
    {
        static readonly TimeSpan TrailingLineArrivalDeadline = TimeSpan.FromMilliseconds(10);
        
        readonly FrameReader _reader;
        readonly JsonSerializer _serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            DateParseHandling = DateParseHandling.None,
            Culture = CultureInfo.InvariantCulture
        });

        public JsonLogEventReader(TextReader input)
        {
            _reader = new FrameReader(
                input ?? throw new ArgumentNullException(nameof(input)),
                Parse.Return(TextSpan.None),
                TrailingLineArrivalDeadline);
        }

        public async Task<ReadResult> TryReadAsync()
        {
            var frame = await _reader.TryReadAsync();
            if (!frame.HasValue)
                return new ReadResult(null, frame.IsAtEnd);

            if (frame.IsOrphan)
                throw new InvalidDataException($"A line arrived late or could not be parsed: `{frame.Value.Trim()}`.");

            var jobject = _serializer.Deserialize<JObject>(new JsonTextReader(new StringReader(frame.Value)));

            if (!jobject.TryGetValue("@t", out _))
                jobject.Add("@t", new JValue(DateTime.UtcNow.ToString("O")));

            // Serilog.Formatting.Compact.Reader issue #13
            if (!jobject.TryGetValue("@m", out _) && !jobject.TryGetValue("@mt", out _))
                jobject.Add("@mt", new JValue(""));

            var evt = LogEventReader.ReadFromJObject(jobject);
            return new ReadResult(evt, frame.IsAtEnd);
        }
    }
}
