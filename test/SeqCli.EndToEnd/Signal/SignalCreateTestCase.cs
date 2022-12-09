using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Signal;

public class SignalCreateTestCase : ICliTestCase
{
    public async Task ExecuteAsync(
        SeqConnection connection,
        ILogger logger,
        CliCommandRunner runner)
    {
        var exit = runner.Exec("signal create", "-t TestSignal -f \"@Exception is not null\" -c Column1 -c \"round(Property2, 1)\"");
        Assert.Equal(0, exit);

        var signals = await connection.Signals.ListAsync(shared: true);
        var testSignal = signals.First(x => x.Title == "TestSignal");

        Assert.Equal("@Exception is not null", testSignal.Filters[0].Filter);
        Assert.Equal("Column1", testSignal.Columns[0].Expression);
        Assert.Equal("round(Property2, 1)", testSignal.Columns[1].Expression);
    }
}