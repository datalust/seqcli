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

    static readonly IReadOnlyDictionary<string, AgentTarget> KnownAgents =
        new Dictionary<string, AgentTarget>
        {
            ["claude"] = new(
                global => global
                    ? Path.Combine(UserProfile, ".claude.json")
                    : Path.Combine(Environment.CurrentDirectory, ".mcp.json"),
                "mcpServers"),

            ["windsurf"] = new(
                global => global
                    ? Path.Combine(UserProfile, ".codeium", "windsurf", "mcp_config.json")
                    : throw new NotSupportedException(
                        "Windsurf only supports a user-global MCP config; re-run with `--global`."),
                "mcpServers"),

            ["vscode"] = new(
                global => global
                    ? Path.Combine(VsCodeUserDir, "mcp.json")
                    : Path.Combine(Environment.CurrentDirectory, ".vscode", "mcp.json"),
                "servers"),
            
            ["copilot"] = new(
                global => global
                    ? Path.Combine(UserProfile, ".copilot", "mcp-config.json")
                    : throw new NotSupportedException(
                        "GitHub Copilot only supports a user-global MCP config; re-run with `--global`."),
                "mcpServers"),

            ["qwen"] = new(
                global => Path.Combine(
                    global ? UserProfile : Environment.CurrentDirectory,
                    ".qwen",
                    "settings.json"),
                "mcpServers"),

            ["gemini"] = new(
                global => Path.Combine(
                    global ? UserProfile : Environment.CurrentDirectory,
                    ".gemini",
                    "settings.json"),
                "mcpServers"),

            ["zed"] = new(
                global => global
                    ? Path.Combine(XdgConfigHome, "zed", "settings.json")
                    : Path.Combine(Environment.CurrentDirectory, ".zed", "settings.json"),
                "context_servers"),

            ["amazonq"] = new(
                global => global
                    ? Path.Combine(UserProfile, ".aws", "amazonq", "mcp.json")
                    : Path.Combine(Environment.CurrentDirectory, ".amazonq", "mcp.json"),
                "mcpServers"),

            ["roo"] = new(
                global => global
                    ? throw new NotSupportedException(
                        "Roo Code stores user-global MCP servers in VS Code extension storage; install into a project instead.")
                    : Path.Combine(Environment.CurrentDirectory, ".roo", "mcp.json"),
                "mcpServers"),

            ["codex"] = Unsupported(
                "Codex reads MCP servers from ~/.codex/config.toml (TOML), which seqcli can't edit automatically. Add this block:\n\n[mcp_servers.seq]\ncommand = \"seqcli\"\nargs = [\"mcp\", \"run\"]"),

            ["goose"] = Unsupported(
                "Goose reads MCP servers from ~/.config/goose/config.yaml (YAML) under `extensions`, which seqcli can't edit automatically. Add:\n\nextensions:\n  seq:\n    type: stdio\n    cmd: seqcli\n    args: [mcp, run]\n    enabled: true"),

            ["continue"] = Unsupported(
                "Continue reads MCP servers from YAML, which seqcli can't edit automatically. Create .continue/mcpServers/seq.yaml with:\n\nname: Seq\nversion: 0.0.1\nschema: v1\nmcpServers:\n  - name: seq\n    command: seqcli\n    args:\n      - mcp\n      - run"),
        };
    
    static readonly IReadOnlyDictionary<string, string> AgentAliases =
        new Dictionary<string, string>
        {
            ["github"] = "copilot"
        };

    public static void Install(string? agent, bool global, string? profileName = null)
    {
        agent ??= "agents";

        if (AgentAliases.TryGetValue(agent, out var alias))
            agent = alias;

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

        Console.Write("Installing MCP server to `{0}`...", path);
        
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));

        Console.WriteLine(" Done.");
        
        Log.Information("Installed Seq MCP server for {Agent} to {Path}", agent, path);
    }

    static AgentTarget Unsupported(string message) =>
        new(_ => throw new NotSupportedException(message), "mcpServers");

    static AgentTarget Convention(string agent) =>
        new(
            global => Path.Combine(
                global ? UserProfile : Environment.CurrentDirectory,
                $".{agent}",
                "mcp.json"),
            "mcpServers");

    static string UserProfile => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    static string XdgConfigHome =>
        Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } configHome
            ? configHome
            : Path.Combine(UserProfile, ".config");

    // VS Code keeps per-user data in an OS-specific directory.
    static string VsCodeUserDir =>
        OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Code", "User")
            : OperatingSystem.IsMacOS()
                ? Path.Combine(UserProfile, "Library", "Application Support", "Code", "User")
                : Path.Combine(XdgConfigHome, "Code", "User");

    sealed record AgentTarget(Func<bool, string> ResolvePath, string ServerMapKey);
}
