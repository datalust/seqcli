using System.IO;
using System.Threading.Tasks;
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

        var exit = runner.Exec("skills install -a test-agent", disconnected: true, workingDirectory: tmp.Path);
        Assert.Equal(0, exit);
        Assert.True(File.Exists(Path.Combine(tmp.Path, ".test-agent/skills/seq-search-and-query/SKILL.md")));

        return Task.CompletedTask;
    }
}