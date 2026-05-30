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

using System.Threading.Tasks;
using SeqCli.Mcp;
using SeqCli.Util;

namespace SeqCli.Cli.Commands.Mcp;

[Command("mcp", "install", "Install or update the Seq MCP server for an agent",
    Example = "seqcli mcp install --global --agent claude")]
class InstallCommand : Command
{
    bool _global;
    string? _agent;
    string? _profile;

    public InstallCommand()
    {
        Options.Add(
            "g|global",
            "Install for the current user globally; the default is to install into the current project directory",
            _ => _global = true);

        Options.Add(
            "a=|agent=",
            "The agent name to install the MCP server for; the default is the generic name `agents`",
            t => _agent = ArgumentString.Normalize(t));

        Options.Add(
            "profile=",
            "A connection profile to use; by default the `connection.serverUrl` and `connection.apiKey` config values will be used",
            v => _profile = ArgumentString.Normalize(v));
    }

    protected override Task<int> Run()
    {
        McpServerInstaller.Install(_agent?.ToLowerInvariant() ?? "agents", _global, _profile);
        return Task.FromResult(0);
    }
}
