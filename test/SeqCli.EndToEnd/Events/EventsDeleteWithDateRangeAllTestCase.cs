using System;
using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Delete;

public class EventsDeleteWithDateRangeAllTestCase : ICliTestCase
{
    readonly TestDataFolder _testDataFolder;

    public EventsDeleteWithDateRangeAllTestCase(TestDataFolder testDataFolder)
    {
        _testDataFolder = testDataFolder;
    }

    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var inputFile = _testDataFolder.ItemPath("delete-date-range-all.clef");

        var isoNow = DateTime.UtcNow.ToString("o");
        await File.WriteAllTextAsync(inputFile,
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":1}}" + Environment.NewLine +
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":2}}" + Environment.NewLine +
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":3}}");
        var exit = runner.Exec("ingest", $"-i \"{inputFile}\"");
        Assert.Equal(0, exit);

        var eventsBefore = await connection.Events.ListAsync();
        Assert.Equal(3, eventsBefore.Count);

        var isoFrom = DateTime.UtcNow.AddDays(-1).ToString("o");
        var isoTo = DateTime.UtcNow.AddDays(1).ToString("o");
        exit = runner.Exec("events delete", $"--start={isoFrom} --end={isoTo}");
        Assert.Equal(0, exit);

        var eventsAfter = await connection.Events.ListAsync();
        Assert.Empty(eventsAfter);
    }
}
