// Copyright Datalust Pty Ltd and Contributors
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
using System.Linq;
using System.Threading.Tasks;
using SeqCli.Cli.Features;
using SeqCli.Connection;
using SeqCli.Util;
using Serilog;

namespace SeqCli.Cli.Commands.Node;

[Command("node", "health",
    "Probe a Seq node's `/health` endpoint, and print the returned HTTP status code, or 'Unreachable' if the endpoint could not be queried",
    Example = "seqcli node health -s https://seq-2.example.com")]
class HealthCommand : Command
{
    readonly SeqConnectionFactory _connectionFactory;

    string? _profileName, _serverUrl;

    public HealthCommand(SeqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
            
        Options.Add("s=|server=",
            "The URL of the Seq server; by default the `connection.serverUrl` config value will be used",
            v => _serverUrl = ArgumentString.Normalize(v));

        Options.Add("profile=",
            "A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used",
            v => _profileName = ArgumentString.Normalize(v));
    }

    protected override async Task<int> Run()
    {
        // An API key is not accepted; we don't want to imply that /health requires authentication.
        var surrogateConnectionFeature = new ConnectionFeature { ProfileName = _profileName, Url = _serverUrl };
        var connection = _connectionFactory.Connect(surrogateConnectionFeature);

        try
        {
            var response = await connection.Client.HttpClient.GetAsync("health");
            Log.Information("HTTP {HttpVersion} {StatusCode} {ReasonPhrase}", response.Version, (int)response.StatusCode, response.ReasonPhrase);
            
            foreach (var (key, values) in response.Headers.Concat(response.Content.Headers))
            foreach (var value in values)
            {
                Log.Information("{HeaderName}: {HeaderValue}", key, value);
            }
            
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Exception thrown when calling health endpoint");
                
            Console.WriteLine("Unreachable");
            Console.WriteLine(Presentation.FormattedMessage(ex));
            return 1;
        }
    }
}