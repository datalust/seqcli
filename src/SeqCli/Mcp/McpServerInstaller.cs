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
    // including the default `agents` name, Cursor, and any unknown agent - uses
    // the convention (see `Convention`), so adding support for a conformant agent
    // requires no change at all, and a divergent one is a single entry here. Agents
    // whose config is a format we can't safely edit (TOML/YAML) are listed via
    // `Unsupported` so the user gets a copy-paste snippet instead of an ignored file.
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

            // Windsurf only reads a single user-global config under `~/.codeium`; it has
            // no project-level MCP file, so a project install would be silently ignored.
            ["windsurf"] = new(
                global => global
                    ? Path.Combine(UserProfile, ".codeium", "windsurf", "mcp_config.json")
                    : throw new NotSupportedException(
                        "Windsurf only supports a user-global MCP config; re-run with `--global` (seqcli mcp install --global --agent windsurf)."),
                "mcpServers"),

            // VS Code nests servers under a `servers` key. Project config lives in
            // `.vscode/mcp.json`; the user-global equivalent is a `mcp.json` in the
            // VS Code user directory (`%APPDATA%\Code\User` on Windows, `~/Library/
            // Application Support/Code/User` on macOS, `$XDG_CONFIG_HOME/Code/User` otherwise).
            ["vscode"] = new(
                global => global
                    ? Path.Combine(VsCodeUserDir, "mcp.json")
                    : Path.Combine(Environment.CurrentDirectory, ".vscode", "mcp.json"),
                "servers"),

            // Qwen Code reads MCP servers from the `mcpServers` key of its `settings.json`,
            // both user-global (`~/.qwen`) and per-project (`.qwen`) - not a standalone `mcp.json`.
            ["qwen"] = new(
                global => Path.Combine(
                    global ? UserProfile : Environment.CurrentDirectory,
                    ".qwen",
                    "settings.json"),
                "mcpServers"),

            // Gemini CLI mirrors Qwen Code: `mcpServers` inside `settings.json` under `.gemini`.
            ["gemini"] = new(
                global => Path.Combine(
                    global ? UserProfile : Environment.CurrentDirectory,
                    ".gemini",
                    "settings.json"),
                "mcpServers"),

            // Zed embeds servers in its `settings.json` under a `context_servers` key
            // (project `.zed/settings.json`; user-global `$XDG_CONFIG_HOME/zed/settings.json`).
            ["zed"] = new(
                global => global
                    ? Path.Combine(XdgConfigHome, "zed", "settings.json")
                    : Path.Combine(Environment.CurrentDirectory, ".zed", "settings.json"),
                "context_servers"),

            // Amazon Q Developer CLI uses a standalone `mcp.json`: `.amazonq` per-project,
            // but `~/.aws/amazonq` for the user-global file.
            ["amazonq"] = new(
                global => global
                    ? Path.Combine(UserProfile, ".aws", "amazonq", "mcp.json")
                    : Path.Combine(Environment.CurrentDirectory, ".amazonq", "mcp.json"),
                "mcpServers"),

            // Roo Code reads a project `.roo/mcp.json`; its user-global store lives in
            // VS Code extension storage, whose path is publisher/platform-specific.
            ["roo"] = new(
                global => global
                    ? throw new NotSupportedException(
                        "Roo Code stores user-global MCP servers in VS Code extension storage; install into a project instead (seqcli mcp install --agent roo).")
                    : Path.Combine(Environment.CurrentDirectory, ".roo", "mcp.json"),
                "mcpServers"),

            // Codex, Goose, and Continue store MCP config in TOML/YAML that seqcli can't
            // safely edit, so we print the exact config to add by hand rather than writing
            // a JSON file the agent would ignore.
            ["codex"] = Unsupported(
                "Codex reads MCP servers from ~/.codex/config.toml (TOML), which seqcli can't edit automatically. Add this block:\n\n[mcp_servers.seq]\ncommand = \"seqcli\"\nargs = [\"mcp\", \"run\"]"),

            ["goose"] = Unsupported(
                "Goose reads MCP servers from ~/.config/goose/config.yaml (YAML) under `extensions`, which seqcli can't edit automatically. Add:\n\nextensions:\n  seq:\n    type: stdio\n    cmd: seqcli\n    args: [mcp, run]\n    enabled: true"),

            ["continue"] = Unsupported(
                "Continue reads MCP servers from YAML, which seqcli can't edit automatically. Create .continue/mcpServers/seq.yaml with:\n\nname: Seq\nversion: 0.0.1\nschema: v1\nmcpServers:\n  - name: seq\n    command: seqcli\n    args:\n      - mcp\n      - run"),
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

    // For agents whose config format we can't write, resolving any path throws with a
    // copy-paste snippet; the command runner turns this into a clean exit-1 message.
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
