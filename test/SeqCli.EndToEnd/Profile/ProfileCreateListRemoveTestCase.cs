using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Profile;

public class ProfileCreateListRemoveTestCase : ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var create = runner.Exec("profile create", "-n Test -s https://seq.example.com -a 123", disconnected: true);
        Assert.Equal(0, create);

        Assert.Equal(0, runner.Exec("profile list", disconnected: true));
        Assert.Contains("test (https://seq.example.com)", runner.LastRunProcess.Output);

        Assert.Equal(0, runner.Exec("config", disconnected: true));
        Assert.Contains("profiles[test].serverUrl", runner.LastRunProcess.Output);
        Assert.Contains("https://seq.example.com", runner.LastRunProcess.Output);
        Assert.Contains("profiles[test].apiKey", runner.LastRunProcess.Output);
        Assert.Contains("123", runner.LastRunProcess.Output);

        var remove = runner.Exec("profile remove", "-n Test", disconnected: true);
        Assert.Equal(0, remove);

        return Task.CompletedTask;
    }
}