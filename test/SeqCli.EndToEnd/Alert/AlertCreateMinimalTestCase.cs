using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using Seq.Api.Model.Shared;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Alert;

public class AlertCreateMinimalTestCase : ICliTestCase
{
    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        // The minimal invocation: only a title is supplied, and every other field falls
        // back to the server-provided alert template.
        var exit = runner.Exec("alert create", "-t MinimalAlert");
        Assert.Equal(0, exit);

        var alert = (await connection.Alerts.ListAsync(shared: true)).First(x => x.Title == "MinimalAlert");

        Assert.Equal("MinimalAlert", alert.Title);
        Assert.Equal(DataSource.Stream, alert.DataSource);

        exit = runner.Exec("alert list", "-t MinimalAlert");
        Assert.Equal(0, exit);
    }
}
