using System;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using Seq.Api.Model.LogEvents;
using Seq.Api.Model.Shared;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Alert;

public class AlertCreateAllArgsTestCase : ICliTestCase
{
    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        // An app instance is required to exercise `--notification-app`.
        var exit = runner.Exec("app install", "--package-id Seq.App.EmailPlus");
        Assert.Equal(0, exit);

        var app = (await connection.Apps.ListAsync()).Single();

        exit = runner.Exec("appinstance create",
            $"-t AlertNotify --app {app.Id} -p To=example@example.com -p From=example@example.com -p Host=localhost");
        Assert.Equal(0, exit);

        var appInstance = (await connection.AppInstances.ListAsync()).Single();

        // Every option the `alert create` command accepts, including the repeatable
        // `--select`, `--group-by`, and `--notification-app` arguments.
        exit = runner.Exec("alert create",
            "-t AllArgsAlert --description \"Covers every alert create option\" --from stream " +
            "--signal signal-m33301 --where \"@Level = 'Error'\" " +
            "--select \"count(*) as errors\" --select \"max(Elapsed) as slowest\" " +
            "--group-by \"ServiceName ci as service\" --group-by \"Environment\" " +
            "--window 5m --having \"errors > 10\" --notification-level Error --suppression-time 10m " +
            $"--notification-app {appInstance.Id} --protected --disabled");
        Assert.Equal(0, exit);

        var testAlert = (await connection.Alerts.ListAsync(shared: true)).First(x => x.Title == "AllArgsAlert");

        Assert.Equal("Covers every alert create option", testAlert.Description);
        Assert.Equal(DataSource.Stream, testAlert.DataSource);
        Assert.NotNull(testAlert.SignalExpression);
        Assert.False(string.IsNullOrWhiteSpace(testAlert.Where));
        Assert.Equal(2, testAlert.Select.Count);
        Assert.Equal("errors", testAlert.Select[0].Label);
        Assert.Equal("slowest", testAlert.Select[1].Label);
        Assert.Equal(2, testAlert.GroupBy.Count);
        Assert.Equal("ServiceName", testAlert.GroupBy[0].Value);
        Assert.Equal("service", testAlert.GroupBy[0].Label);
        Assert.True(testAlert.GroupBy[0].IsCaseInsensitive);
        Assert.Equal("Environment", testAlert.GroupBy[1].Value);
        Assert.False(testAlert.GroupBy[1].IsCaseInsensitive);
        Assert.Equal(TimeSpan.FromMinutes(5), testAlert.TimeGrouping);
        Assert.False(string.IsNullOrWhiteSpace(testAlert.Having));
        Assert.Equal(LogEventLevel.Error, testAlert.NotificationLevel);
        Assert.Equal(TimeSpan.FromMinutes(10), testAlert.SuppressionTime);
        Assert.Contains(testAlert.NotificationChannels, c => c.NotificationAppInstanceId == appInstance.Id);
        Assert.True(testAlert.IsProtected);
        Assert.True(testAlert.IsDisabled);

        // `--from series` selects the alternative data source, and `--lateral` (a lateral join)
        // unnests histogram buckets from it; these belong to the metrics-oriented `series` source
        // rather than the stream-oriented `--signal` above, so they're exercised separately.
        exit = runner.Exec("alert create",
            "-t SeriesAlert --from series --lateral \"unnest(commit_duration.buckets) as bucket\" " +
            "--where \"Has(commit_duration.buckets)\" " +
            "--select \"phist(bucket.midpoint, bucket.count, 95) as p95\" " +
            "--window 5m --having \"p95 > 10\" --notification-level Error --suppression-time 10m");
        Assert.Equal(0, exit);

        var seriesAlert = (await connection.Alerts.ListAsync(shared: true)).First(x => x.Title == "SeriesAlert");
        Assert.Equal(DataSource.Series, seriesAlert.DataSource);
        var lateralJoin = Assert.Single(seriesAlert.Joins);
        Assert.Equal(JoinKind.Lateral, lateralJoin.Kind);
        Assert.Equal("unnest(commit_duration.buckets)", lateralJoin.SetFunctionCall);
        Assert.Equal("bucket", lateralJoin.Alias);
    }
}
