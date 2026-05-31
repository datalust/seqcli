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

        // VS Code has no supported user-global merge target.
        var vscodeGlobalExit = runner.Exec("mcp install -a vscode --global", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(1, vscodeGlobalExit);

        var vscodeGlobalOutput = runner.LastRunProcess!.Output;
        Assert.Contains("VS Code stores user-level MCP servers", vscodeGlobalOutput);
        Assert.Contains("seqcli mcp install --agent vscode", vscodeGlobalOutput);
        Assert.DoesNotContain("NotSupportedException", vscodeGlobalOutput);

        return Task.CompletedTask;
    }
}
