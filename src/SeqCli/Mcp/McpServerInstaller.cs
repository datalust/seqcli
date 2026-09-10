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
using System.Linq;
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
                $"Codex reads MCP servers from ~/.codex/config.toml (TOML), which seqcli can't edit automatically. Add this block:{Environment.NewLine}{Environment.NewLine}[mcp_servers.seq]{Environment.NewLine}command = \"seqcli\"{Environment.NewLine}args = [\"mcp\", \"run\"]"),

            ["goose"] = Unsupported(
                $"Goose reads MCP servers from ~/.config/goose/config.yaml (YAML) under `extensions`, which seqcli can't edit automatically. Add:{Environment.NewLine}{Environment.NewLine}extensions:{Environment.NewLine}  seq:{Environment.NewLine}    type: stdio{Environment.NewLine}    cmd: seqcli{Environment.NewLine}    args: [mcp, run]{Environment.NewLine}    enabled: true"),

            ["continue"] = Unsupported(
                $"Continue reads MCP servers from YAML, which seqcli can't edit automatically. Create .continue/mcpServers/seq.yaml with:{Environment.NewLine}{Environment.NewLine}name: Seq{Environment.NewLine}version: 0.0.1{Environment.NewLine}schema: v1{Environment.NewLine}mcpServers:{Environment.NewLine}  - name: seq{Environment.NewLine}    command: seqcli{Environment.NewLine}    args:{Environment.NewLine}      - mcp{Environment.NewLine}      - run"),
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

        var (command, leadingArgs) = ResolveCommand();

        // A connection profile is the only connection setting we propagate; the server URL and
        // API key are resolved from config at runtime so they're not baked into the agent's file.
        var args = new JArray(leadingArgs.Concat(["mcp", "run"]).ToArray<object>());
        if (profileName != null)
        {
            args.Add("--profile");
            args.Add(profileName);
        }

        serverMap[ServerName] = new JObject
        {
            ["command"] = command,
            ["args"] = args,
        };

        Console.Write("Installing MCP server to `{0}`...", path);
        
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, root.ToString(Newtonsoft.Json.Formatting.Indented));

        Console.WriteLine(" Done.");
        
        Log.Information("Installed Seq MCP server for {Agent} to {Path}", agent, path);
    }

    // Agents resolve `seqcli` from PATH when they start the server. On Windows, an npm-installed
    // `seqcli` is a `seqcli.cmd` shim, which hosts that spawn processes without a shell can't run
    // directly, so in that case the server is launched through `cmd /c` instead.
    static (string Command, string[] LeadingArgs) ResolveCommand() =>
        ResolveCommand(
            OperatingSystem.IsWindows(),
            Environment.GetEnvironmentVariable("PATH"),
            Environment.GetEnvironmentVariable("PATHEXT"),
            File.Exists);

    internal static (string Command, string[] LeadingArgs) ResolveCommand(
        bool isWindows,
        string? path,
        string? pathExt,
        Func<string, bool> fileExists)
    {
        if (!isWindows)
            return ("seqcli", []);

        var found = FindOnWindowsPath("seqcli", path, pathExt, fileExists);
        if (found == null)
            return ("seqcli", []);

        var extension = Path.GetExtension(found);
        if (extension.Equals(".cmd", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".bat", StringComparison.OrdinalIgnoreCase))
        {
            Log.Information("Found `seqcli` on PATH as {ShimPath}; the MCP server will be launched via `cmd /c`", found);
            return ("cmd", ["/c", "seqcli"]);
        }

        return ("seqcli", []);
    }

    // Mirrors how Windows locates a command: each PATH directory in turn, trying the PATHEXT
    // extensions in order within it.
    static string? FindOnWindowsPath(string name, string? path, string? pathExt, Func<string, bool> fileExists)
    {
        var extensions = (pathExt is { Length: > 0 } ? pathExt : ".COM;.EXE;.BAT;.CMD")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var directory in (path ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var extension in extensions)
            {
                var candidate = Path.Combine(directory, name + extension);
                if (fileExists(candidate))
                    return candidate;
            }
        }

        return null;
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
