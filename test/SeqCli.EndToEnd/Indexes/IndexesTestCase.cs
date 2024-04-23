using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Indexes;

[CliTestCase(MinimumApiVersion = "2024.3.0")]
public class IndexesTestCase: ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        const string expr = "Magic123";
        var exit = runner.Exec("expressionindex create", $"-e {expr}");
        Assert.Equal(0, exit);

        var expressionIndex = (await connection.ExpressionIndexes.ListAsync()).Single(e => e.Expression == expr);
        var signal = (await connection.Signals.ListAsync()).First(s => !s.IsIndexSuppressed);
        var indexForSignal = (await connection.Indexes.ListAsync()).First(i => i.IndexedEntityId == signal.Id);

        exit = runner.Exec("index list");
        Assert.Equal(0, exit);
        Assert.Contains(expressionIndex.Id, runner.LastRunProcess!.Output);
        Assert.Contains(signal.Id, runner.LastRunProcess!.Output);

        exit = runner.Exec($"index suppress -i {indexForSignal.Id}");
        Assert.Equal(0, exit);

        signal = await connection.Signals.FindAsync(signal.Id);
        Assert.True(signal.IsIndexSuppressed);
    }
}
