using System;
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
        var previous = Environment.CurrentDirectory;
        Environment.CurrentDirectory = tmp.Path;
        try
        {
            var exit = runner.Exec("skills install -a test-agent");
            Assert.Equal(0, exit);
            Assert.True(File.Exists(Path.Combine(tmp.Path, ".test-agent/skills/seq-query-grammar/SKILL.md")));
        }
        finally
        {
            Environment.CurrentDirectory = previous;
        }

        return Task.CompletedTask;
    }
}