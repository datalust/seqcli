using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Mcp;

// ReSharper disable once UnusedType.Global
public class McpInstallTestCase : ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        using var tmp = new TestDataFolder();

        // Convention fallback: an agent that isn't specially known writes `.{agent}/mcp.json`
        // with the common `mcpServers` shape pointing at a bare `mcp run`.
        var exit = runner.Exec("mcp install -a test-agent", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, exit);

        var config = File.ReadAllText(Path.Combine(tmp.Path, ".test-agent/mcp.json"));
        Assert.Contains("\"mcpServers\"", config);
        Assert.Contains("\"seq\"", config);
        Assert.Contains("\"seqcli\"", config);

        // Known-agent override: Claude Code reads a root `.mcp.json`, not `.claude/mcp.json`.
        var claudeExit = runner.Exec("mcp install -a claude", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, claudeExit);
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".mcp.json")));
        Assert.False(File.Exists(Path.Combine(tmp.Path, ".claude/mcp.json")));

        // Merge preserves any server already present rather than overwriting the file.
        var mergePath = Path.Combine(tmp.Path, ".merge-agent/mcp.json");
        Directory.CreateDirectory(Path.GetDirectoryName(mergePath)!);
        File.WriteAllText(mergePath, "{\"mcpServers\":{\"other\":{\"command\":\"x\"}}}");

        var mergeExit = runner.Exec("mcp install -a merge-agent", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, mergeExit);

        var merged = File.ReadAllText(mergePath);
        Assert.Contains("\"other\"", merged);
        Assert.Contains("\"seq\"", merged);

        // A `--profile` is propagated onto the generated server's `mcp run` args; other
        // connection settings (server URL, API key) are deliberately left to runtime config.
        var profileExit = runner.Exec("mcp install -a profile-agent --profile Production", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, profileExit);

        var profileConfig = File.ReadAllText(Path.Combine(tmp.Path, ".profile-agent/mcp.json"));
        Assert.Contains("\"--profile\"", profileConfig);
        Assert.Contains("\"Production\"", profileConfig);

        // Qwen Code reads MCP servers from `mcpServers` in its `settings.json`, not an `mcp.json`.
        var qwenExit = runner.Exec("mcp install -a qwen", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, qwenExit);

        var qwenConfig = File.ReadAllText(Path.Combine(tmp.Path, ".qwen/settings.json"));
        Assert.Contains("\"mcpServers\"", qwenConfig);
        Assert.Contains("\"seq\"", qwenConfig);
        Assert.False(File.Exists(Path.Combine(tmp.Path, ".qwen/mcp.json")));

        // VS Code nests servers under a `servers` key in `.vscode/mcp.json`.
        var vscodeExit = runner.Exec("mcp install -a vscode", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, vscodeExit);

        var vscodeConfig = File.ReadAllText(Path.Combine(tmp.Path, ".vscode/mcp.json"));
        Assert.Contains("\"servers\"", vscodeConfig);
        Assert.Contains("\"seq\"", vscodeConfig);

        // Gemini CLI reads `mcpServers` from `.gemini/settings.json`, not an `mcp.json`.
        var geminiExit = runner.Exec("mcp install -a gemini", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, geminiExit);

        var geminiConfig = File.ReadAllText(Path.Combine(tmp.Path, ".gemini/settings.json"));
        Assert.Contains("\"mcpServers\"", geminiConfig);
        Assert.Contains("\"seq\"", geminiConfig);
        Assert.False(File.Exists(Path.Combine(tmp.Path, ".gemini/mcp.json")));

        // Zed embeds servers under `context_servers` in `.zed/settings.json`.
        var zedExit = runner.Exec("mcp install -a zed", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, zedExit);

        var zedConfig = File.ReadAllText(Path.Combine(tmp.Path, ".zed/settings.json"));
        Assert.Contains("\"context_servers\"", zedConfig);
        Assert.Contains("\"seq\"", zedConfig);

        // Amazon Q Developer CLI reads a project `.amazonq/mcp.json`.
        var amazonqExit = runner.Exec("mcp install -a amazonq", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, amazonqExit);

        var amazonqConfig = File.ReadAllText(Path.Combine(tmp.Path, ".amazonq/mcp.json"));
        Assert.Contains("\"mcpServers\"", amazonqConfig);
        Assert.Contains("\"seq\"", amazonqConfig);

        // Roo Code reads a project `.roo/mcp.json`...
        var rooExit = runner.Exec("mcp install -a roo", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, rooExit);
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".roo/mcp.json")));

        // ...but has no writable user-global target, so `--global` reports a clean error
        // (and never leaks the exception type into the output).
        var rooGlobalExit = runner.Exec("mcp install -a roo --global", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(1, rooGlobalExit);

        var rooGlobalOutput = runner.LastRunProcess!.Output;
        Assert.Contains("extension storage", rooGlobalOutput);
        Assert.DoesNotContain("NotSupportedException", rooGlobalOutput);

        // Windsurf is user-global only; a project install is rejected rather than writing
        // an ignored `.windsurf/mcp.json`.
        var windsurfExit = runner.Exec("mcp install -a windsurf", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(1, windsurfExit);
        Assert.Contains("--global", runner.LastRunProcess!.Output);
        Assert.False(File.Exists(Path.Combine(tmp.Path, ".windsurf/mcp.json")));

        // Codex/Goose/Continue use TOML/YAML config seqcli can't edit; instead of writing
        // an ignored JSON file, the command prints a copy-paste snippet and fails.
        var codexExit = runner.Exec("mcp install -a codex", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(1, codexExit);
        Assert.Contains("config.toml", runner.LastRunProcess!.Output);
        Assert.False(Directory.Exists(Path.Combine(tmp.Path, ".codex")));

        var gooseExit = runner.Exec("mcp install -a goose", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(1, gooseExit);
        Assert.Contains("config.yaml", runner.LastRunProcess!.Output);
        Assert.False(Directory.Exists(Path.Combine(tmp.Path, ".goose")));

        var continueExit = runner.Exec("mcp install -a continue", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(1, continueExit);
        Assert.Contains("YAML", runner.LastRunProcess!.Output);
        Assert.False(File.Exists(Path.Combine(tmp.Path, ".continue/mcp.json")));

        return Task.CompletedTask;
    }
}
