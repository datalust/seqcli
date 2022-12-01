using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Bench
{
    public class BenchTestCase : ICliTestCase
    {
        public Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var exit = runner.Exec("bench", "--start=2022-01-01 --end=2022-01-02");
            Assert.Equal(0, exit);

            return Task.CompletedTask;
        }
    }
}
