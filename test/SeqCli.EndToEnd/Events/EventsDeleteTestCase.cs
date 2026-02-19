using System;
using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Delete;

public class EventsDeleteTestCase : ICliTestCase
{
    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {

        var inputFile = Path.Combine("Data", "events.clef");
        Assert.True(File.Exists(inputFile));

        var exit = runner.Exec("ingest", $"-i {inputFile}");
        Assert.Equal(0, exit);

        var eventsBefore = await connection.Events.ListAsync();
        Assert.Equal(15, eventsBefore.Count);

        exit = runner.Exec("events delete");
        Assert.Equal(0, exit);

        var eventsAfter = await connection.Events.ListAsync();
        Assert.Empty(eventsAfter);
    }
}
