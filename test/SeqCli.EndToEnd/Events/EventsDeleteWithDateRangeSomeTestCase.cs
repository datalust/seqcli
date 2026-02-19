using System;
using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Delete;

public class EventsDeleteWithDateRangeSomeTestCase : ICliTestCase
{
    readonly TestDataFolder _testDataFolder;

    public EventsDeleteWithDateRangeSomeTestCase(TestDataFolder testDataFolder)
    {
        _testDataFolder = testDataFolder;
    }

    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var inputFile = _testDataFolder.ItemPath("delete-date-range-none.clef");

        var isoNow = DateTime.UtcNow.ToString("o");
        await File.WriteAllTextAsync(inputFile,
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":1}}" + Environment.NewLine +
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":2}}" + Environment.NewLine +
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":3}}");
        var exit = runner.Exec("ingest", $"-i \"{inputFile}\"");
        Assert.Equal(0, exit);

        isoNow = DateTime.UtcNow.ToString("o");
        await File.WriteAllTextAsync(inputFile,
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":4}}" + Environment.NewLine +
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":5}}" + Environment.NewLine +
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":6}}");
        exit = runner.Exec("ingest", $"-i \"{inputFile}\"");
        Assert.Equal(0, exit);

        var eventsBefore = await connection.Events.ListAsync();
        Assert.Equal(6, eventsBefore.Count);

        var isoTo = DateTime.UtcNow.AddDays(1).ToString("o");
        exit = runner.Exec("events delete", $"--start={isoNow} --end={isoTo}");
        Assert.Equal(0, exit);

        var eventsAfter = await connection.Events.ListAsync();
        Assert.Equal(3, eventsAfter.Count);
    }
}
