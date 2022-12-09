using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.ApiKey;

public class ApiKeyCreateTestCase : ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var exit = runner.Exec("apikey create", "-t Test");
        Assert.Equal(0, exit);

        exit = runner.Exec("apikey list", "-t Test --json --no-color");
        Assert.Equal(0, exit);

        var output = runner.LastRunProcess!.Output;
        Assert.Contains("\"AssignedPermissions\": [\"Ingest\"]", output);

        return Task.CompletedTask;
    }
}