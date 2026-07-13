using System;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using Seq.Api.Model.LogEvents;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Alert;

public class AlertCreateTestCase : ICliTestCase
{
    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var exit = runner.Exec("alert create",
            "-t TestAlert --where \"@Level = 'Error'\" --select \"count(*) as errors\" " +
            "--window 5m --having \"errors > 10\" --level Error --suppression-time 10m");
        Assert.Equal(0, exit);

        var alerts = await connection.Alerts.ListAsync(shared: true);
        var testAlert = alerts.First(x => x.Title == "TestAlert");

        Assert.Equal("errors", testAlert.Select[0].Label);
        Assert.Equal(TimeSpan.FromMinutes(5), testAlert.TimeGrouping);
        Assert.Equal(TimeSpan.FromMinutes(10), testAlert.SuppressionTime);
        Assert.Equal(LogEventLevel.Error, testAlert.NotificationLevel);
        Assert.False(string.IsNullOrWhiteSpace(testAlert.Where));
        Assert.False(string.IsNullOrWhiteSpace(testAlert.Having));
    }
}
