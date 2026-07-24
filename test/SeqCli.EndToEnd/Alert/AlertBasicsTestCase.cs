using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Alert;

[CliTestCase(MinimumApiVersion = "2026.1.0")]
public class AlertBasicsTestCase : ICliTestCase
{
    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var alert = await connection.Alerts.TemplateAsync();
        alert.Title = "TestBasicsAlert";
        alert.OwnerId = null;
        alert = await connection.Alerts.AddAsync(alert);

        var exit = runner.Exec("alert list", "-i alert-none");
        Assert.Equal(1, exit);

        exit = runner.Exec("alert list", "-t TestBasicsAlert");
        Assert.Equal(0, exit);

        var output = runner.LastRunProcess?.Output;
        Assert.Equal($"{alert.Id} TestBasicsAlert", output?.Trim());

        exit = runner.Exec("alert disable", "-t TestBasicsAlert");
        Assert.Equal(0, exit);

        alert = await connection.Alerts.FindAsync(alert.Id);
        Assert.True(alert.IsDisabled);

        exit = runner.Exec("alert enable", "-t TestBasicsAlert");
        Assert.Equal(0, exit);

        alert = await connection.Alerts.FindAsync(alert.Id);
        Assert.False(alert.IsDisabled);

        exit = runner.Exec("alert remove", "-t TestBasicsAlert");
        Assert.Equal(0, exit);

        exit = runner.Exec("alert list", $"-i {alert.Id}");
        Assert.Equal(1, exit);
    }
}
