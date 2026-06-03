using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Skills;

public class SkillsInstallTestCase : ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        using var tmp = new TestDataFolder();

        // Convention fallback: an agent that isn't specially known installs into `.{agent}/skills`.
        var exit = runner.Exec("skills install -a test-agent", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, exit);
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".test-agent/skills/seq-search-and-query/SKILL.md")));

        // Claude Code reads `.claude/skills`, and refuses the portable `.agents` alias, so it must keep its own namespace.
        var claudeExit = runner.Exec("skills install -a claude", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, claudeExit);
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".claude/skills/seq-search-and-query/SKILL.md")));

        // Codex has no `.codex` skills dir; its project skills live in the portable `.agents/skills`.
        var codexExit = runner.Exec("skills install -a codex", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, codexExit);
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".agents/skills/seq-search-and-query/SKILL.md")));
        Assert.False(Directory.Exists(Path.Combine(tmp.Path, ".codex")));

        // GitHub Copilot / VS Code read workspace skills from `.github/skills`, not `.copilot/skills`.
        var copilotExit = runner.Exec("skills install -a copilot", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, copilotExit);
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".github/skills/seq-search-and-query/SKILL.md")));
        Assert.False(Directory.Exists(Path.Combine(tmp.Path, ".copilot")));

        // `github` is an alias for the same Copilot workspace location.
        var githubExit = runner.Exec("skills install -a github", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, githubExit);
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".github/skills/seq-search-and-query/SKILL.md")));

        // Goose uses the `agents` convention.
        var gooseExit = runner.Exec("skills install -a goose", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, gooseExit);
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".agents/skills/seq-search-and-query/SKILL.md")));

        return Task.CompletedTask;
    }
}