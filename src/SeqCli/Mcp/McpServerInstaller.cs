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
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Serilog;

namespace SeqCli.Mcp;

static class McpServerInstaller
{
    const string ServerName = "seq";

    // Agents whose MCP config location or shape diverges from the common
    // `.{agent}/mcp.json` + `mcpServers` convention. Anything not listed here -
    // including the default `agents` name and any unknown agent - uses the
    // convention (see `Convention`), so adding support for a conformant agent
    // requires no change at all, and a divergent one is a single entry here.
    static readonly IReadOnlyDictionary<string, AgentTarget> KnownAgents =
        new Dictionary<string, AgentTarget>
        {
            // Claude Code reads project servers from a root `.mcp.json`, and
            // user-global servers from `~/.claude.json`.
            ["claude"] = new(
                global => global
                    ? Path.Combine(UserProfile, ".claude.json")
                    : Path.Combine(Environment.CurrentDirectory, ".mcp.json"),
                "mcpServers"),

            // Windsurf keeps a single user-global config under `~/.codeium`.
            ["windsurf"] = new(
                global => global
                    ? Path.Combine(UserProfile, ".codeium", "windsurf", "mcp_config.json")
                    : Path.Combine(Environment.CurrentDirectory, ".windsurf", "mcp.json"),
                "mcpServers"),

            // VS Code nests servers under a `servers` key. Project config lives in
            // `.vscode/mcp.json`; the user-global equivalent lives inside `settings.json`,
            // which is a different merge target and isn't supported here yet.
            ["vscode"] = new(
                global => global
                    ? throw new NotSupportedException(
                        "VS Code stores user-level MCP servers in settings.json; install into a project with `seqcli mcp install --agent vscode` instead.")
                    : Path.Combine(Environment.CurrentDirectory, ".vscode", "mcp.json"),
                "servers"),
        };

    public static void Install(string? agent, bool global, string? profileName = null)
    {
        agent ??= "agents";

        var target = KnownAgents.TryGetValue(agent, out var known) ? known : Convention(agent);
        var path = target.ResolvePath(global);

        // Merge into any existing config so other servers and unrelated settings survive.
        var root = File.Exists(path) ? JObject.Parse(File.ReadAllText(path)) : new JObject();

        if (root[target.ServerMapKey] is not JObject serverMap)
        {
            serverMap = new JObject();
            root[target.ServerMapKey] = serverMap;
        }

        // A connection profile is the only connection setting we propagate; the server URL and
        // API key are resolved from config at runtime so they're not baked into the agent's file.
        var args = new JArray("mcp", "run");
        if (profileName != null)
        {
            args.Add("--profile");
            args.Add(profileName);
        }

        serverMap[ServerName] = new JObject
        {
            ["command"] = "seqcli",
            ["args"] = args,
        };

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));

        Log.Information("Installed Seq MCP server for {Agent} to {Path}", agent, path);
    }

    static AgentTarget Convention(string agent) =>
        new(
            global => Path.Combine(
                global ? UserProfile : Environment.CurrentDirectory,
                $".{agent}",
                "mcp.json"),
            "mcpServers");

    static string UserProfile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    sealed record AgentTarget(Func<bool, string> ResolvePath, string ServerMapKey);
}
