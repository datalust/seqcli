using System;
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

using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using Serilog;

namespace SeqCli.Cli.Commands
{
    [Command("log", "Send a structured log event to the server", Example = "seqcli log -m 'Hello, {Name}!' -p Name=World -p App=Test")]
    class LogCommand : Command
    {
        readonly SeqConnectionFactory _connectionFactory;
        readonly PropertiesFeature _properties;
        readonly ConnectionFeature _connection;
        string _message, _level, _timestamp, _exception;

        public LogCommand(SeqConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            Options.Add(
                "m=|message=",
                "A message to associate with the event (the default is to send no message); https://messagetemplates.org syntax is supported",
                v => _message = v);

            Options.Add(
                "l=|level=",
                "The level or severity of the event (the default is `Information`)",
                v => _level = v);

            Options.Add(
                "t=|timestamp=",
                "The event timestamp as ISO-8601 (the current UTC timestamp will be used by default)",
                v => _timestamp = v);

            Options.Add(
                "x=|exception=",
                "Additional exception or error information to send, if any",
                v => _exception = v);

            _properties = Enable<PropertiesFeature>();
            _connection = Enable<ConnectionFeature>();
        }

        protected override async Task<int> Run()
        {
            var payload = new JObject();

            payload["@t"] = string.IsNullOrWhiteSpace(_timestamp) ?
                DateTimeOffset.Now.ToString("o") :
                _timestamp;

            if (_level != null && _level != "Information")
                payload["@l"] = _level;

            if (!string.IsNullOrWhiteSpace(_message))
                payload["@mt"] = _message;

            if (!string.IsNullOrWhiteSpace(_exception))
                payload["@x"] = _exception;

            foreach (var property in _properties.Properties)
            {
                if (string.IsNullOrWhiteSpace(property.Key))
                    continue;

                var name = property.Key.Trim();
                if (name.StartsWith("@"))
                    name = $"@{name}";

                payload[name] = new JValue(property.Value);
            }

            StringContent content;
            using (var builder = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(builder))
            {
                payload.WriteTo(jsonWriter);
                jsonWriter.Flush();
                builder.WriteLine();
                content = new StringContent(builder.ToString(), Encoding.UTF8, ApiConstants.ClefMediatType);
            }

            var connection = _connectionFactory.Connect(_connection);

            var request = new HttpRequestMessage(HttpMethod.Post, ApiConstants.IngestionEndpoint) {Content = content};

            if (_connection.IsApiKeySpecified)
                request.Headers.Add("X-Seq-ApiKey", _connection.ApiKey);
            
            var result = await connection.Client.HttpClient.SendAsync(request);

            if (result.IsSuccessStatusCode)
                return 0;

            var resultJson = await result.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(resultJson))
            {
                try
                {
                    var error = JsonConvert.DeserializeObject<dynamic>(resultJson);

                    Log.Error("Failed with status code {StatusCode}: {ErrorMessage}",
                        result.StatusCode,
                        (string) error.ErrorMessage);
                }
                catch
                {
                    // ignored
                }
            }

            Log.Error("Failed with status code {StatusCode} ({ReasonPhrase})", result.StatusCode, result.ReasonPhrase);
            return 1;
        }
    }
}
