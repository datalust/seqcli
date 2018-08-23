using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Dashboard
{
    public class RenderTestcase : ICliTestCase
    {
        public Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var exit = runner.Exec("dashboard list");
            Assert.Equal(0, exit);

            var id = runner.LastRunProcess.Output.Split(' ')[0];

            exit = runner.Exec("dashboard render", $"-i {id} -c \"All Events\" --last 1d --by 1h");
            Assert.Equal(0, exit);

            var lines = new StringReader(runner.LastRunProcess.Output);
            var firstLine = lines.ReadLine();
            Assert.Equal("\"time\",\"count\"", firstLine);

            return Task.CompletedTask;
        }
    }
}
