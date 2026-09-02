using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Search;

public class SearchWithFilterTestCase : ICliTestCase
{
    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        await DirectIngestion.IngestClef(connection, "'@mt': 'Event {N}', 'N': 1, 'Host': 'xmpweb-01.example.com'");
        await DirectIngestion.IngestClef(connection, "'@mt': 'Event {N}', 'N': 2, 'Host': 'xmpweb-02.example.com'");
        await DirectIngestion.IngestClef(connection, "'@mt': 'Event {N}', 'N': 3, 'Host': 'xmpweb-02.example.com'");

        var exit = runner.Exec("search", "--filter=\"Host = 'xmpweb-02.example.com' and N > 2\" --count=10 --json");
        Assert.Equal(0, exit);

        var results = runner.LastRunProcess!.Output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(JObject.Parse)
            .ToList();

        var evt = Assert.Single(results);
        Assert.Equal(3, evt["N"]!.Value<int>());
        Assert.Equal("xmpweb-02.example.com", evt["Host"]!.Value<string>());
    }
}
