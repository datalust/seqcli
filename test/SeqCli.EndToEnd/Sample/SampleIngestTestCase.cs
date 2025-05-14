using System;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Sample;

public class SampleIngestTestCase : ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        try
        {
            runner.Exec("sample ingest", "--setup --confirm", timeout: TimeSpan.FromSeconds(3));
        }
        catch
        {
            // Ignored
        }

        var events = await connection.Events.ListAsync();
        Assert.NotEmpty(events);

        var sampleWorkspace = (await connection.Workspaces.ListAsync(shared: true))
            .SingleOrDefault(w => w.Title == "Sample");

        Assert.NotNull(sampleWorkspace);
    }
}