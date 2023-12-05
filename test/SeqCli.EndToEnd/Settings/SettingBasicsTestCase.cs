using System;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Settings;

public class SettingBasicsTestCase : ICliTestCase
{
    public Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var exit = runner.Exec("setting names");
        Assert.Equal(0, exit);
        Assert.Contains("InstanceTitle", runner.LastRunProcess!.Output);

        exit = runner.Exec("setting show", "-n instancetitle");
        Assert.Equal(0, exit);
        Assert.Empty(runner.LastRunProcess.Output);

        exit = runner.Exec("setting set", "-n instancetitle -v \"Hello, world!\"");
        Assert.Equal(0, exit);

        exit = runner.Exec("setting show", "-n instancetitle");
        Assert.Equal(0, exit);
        Assert.Equal("Hello, world!" + Environment.NewLine, runner.LastRunProcess.Output);

        exit = runner.Exec("setting clear", "-n instancetitle");
        Assert.Equal(0, exit);

        exit = runner.Exec("setting show", "-n instancetitle");
        Assert.Equal(0, exit);
        Assert.Empty(runner.LastRunProcess.Output);

        return Task.CompletedTask;
    }
}
