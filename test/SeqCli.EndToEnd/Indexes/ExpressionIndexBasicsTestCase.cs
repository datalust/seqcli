using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Indexes;

[CliTestCase(MinimumApiVersion = "2024.3.0")]
public class ExpressionIndexBasicsTestCase: ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        const string expr = "@Resource.service.name";
        var exit = runner.Exec("expressionindex create", $"-e {expr}");
        Assert.Equal(0, exit);

        var entity = (await connection.ExpressionIndexes.ListAsync()).Single(e => e.Expression == expr);
        Assert.Equal(expr, entity.Expression);

        exit = runner.Exec("expressionindex list");
        Assert.Equal(0, exit);

        Assert.Contains(expr, runner.LastRunProcess!.Output);
        Assert.Contains(entity.Id, runner.LastRunProcess.Output);

        exit = runner.Exec("expressionindex remove", $"-i {entity.Id}");
        Assert.Equal(0, exit);
        
        exit = runner.Exec("expressionindex list");
        Assert.Equal(0, exit);

        Assert.DoesNotContain(entity.Id, runner.LastRunProcess.Output);
    }
}