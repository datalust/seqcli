using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.User;

public class UserListTestCase : ICliTestCase
{
    public Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var exit = runner.Exec("user list");
        Assert.Equal(0, exit);

        exit = runner.Exec("user list", "-n admin");
        Assert.Equal(0, exit);

        var output = runner.LastRunProcess.Output;
        Assert.Equal("user-admin admin", output.Trim());

        return Task.CompletedTask;
    }
}