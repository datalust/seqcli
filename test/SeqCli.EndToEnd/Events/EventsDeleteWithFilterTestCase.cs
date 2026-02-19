using System;
using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Delete;

public class EventsDeleteWithFilterTestCase : ICliTestCase
{
    readonly TestDataFolder _testDataFolder;

    public EventsDeleteWithFilterTestCase(TestDataFolder testDataFolder)
    {
        _testDataFolder = testDataFolder;
    }

    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var inputFile = _testDataFolder.ItemPath("delete-with-filter.clef");

        var isoNow = DateTime.UtcNow.ToString("o");
        await File.WriteAllTextAsync(inputFile,
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":1}}" + Environment.NewLine +
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":2}}");
        var hostOne = "xmpweb-01.example.com";
        var exit = runner.Exec("ingest", $"-i \"{inputFile}\" -p \"host={hostOne}\"");
        Assert.Equal(0, exit);

        isoNow = DateTime.UtcNow.ToString("o");
        await File.WriteAllTextAsync(inputFile,
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":3}}" + Environment.NewLine +
            $"{{\"@t\":\"{isoNow}\",\"@mt\":\"Event {{N}}\",\"N\":4}}");
        var hostTwo = "xmpweb-02.example.com";
        exit = runner.Exec("ingest", $"-i \"{inputFile}\" -p \"host={hostTwo}\"");
        Assert.Equal(0, exit);

        var eventsBefore = await connection.Events.ListAsync();
        Assert.Equal(4, eventsBefore.Count);

        exit = runner.Exec("events delete", $"--filter=\"host='{hostTwo}'\"");
        Assert.Equal(0, exit);

        var eventsAfter = await connection.Events.ListAsync();
        Assert.Equal(2, eventsAfter.Count);
    }
}
