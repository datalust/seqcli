using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Signal
{
    public class SignalBasicsTestcase : ICliTestCase
    {
        public Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var exit = runner.Exec("signal list", "-i signal-none");
            Assert.Equal(1, exit);

            exit = runner.Exec("signal list", "-t Warnings");
            Assert.Equal(0, exit);

            var output = runner.LastRunProcess.Output;
            Assert.Equal("signal-m33302 Warnings", output.Trim());

            exit = runner.Exec("signal remove", "-t Warnings");
            Assert.Equal(0, exit);

            exit = runner.Exec("signal list", "-t Warnings");
            Assert.Equal(1, exit);

            return Task.CompletedTask;
        }
    }
}
