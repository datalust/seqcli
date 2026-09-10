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

using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using SeqCli.PlainText.Framing;
using Superpower;
using Superpower.Model;

namespace SeqCli.Ingestion;

class JsonEventReader : IEventReader
{
    static readonly TimeSpan TrailingLineArrivalDeadline = TimeSpan.FromMilliseconds(10);

    readonly FrameReader _reader;

    public JsonEventReader(TextReader input)
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

        return new ReadResult(ReadFromJson(frame.Value), frame.IsAtEnd);
    }

    static JsonObject ReadFromJson(string json)
    {
        if (JsonNode.Parse(json) is not JsonObject eventJson)
            throw new InvalidDataException($"The line is not a JSON object: `{json.Trim()}`.");

        if (!eventJson.ContainsKey("@t"))
            eventJson["@t"] = DateTime.UtcNow;
        
        return eventJson;
    }
}
