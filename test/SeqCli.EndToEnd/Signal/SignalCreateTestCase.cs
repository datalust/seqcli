using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Signal
{
    public class SignalCreateTestCase : ICliTestCase
    {
        public Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var exit = runner.Exec("signal create", "-t TestSignal -f \"@Exception is not null\" -c Column1 -c \"round(Property2, 1)\"");
            Assert.Equal(0, exit);

            exit = runner.Exec("signal list", "-t TestSignal --json");
            Assert.Equal(0, exit);

            var output = runner.LastRunProcess.Output.Trim();

            Assert.Contains("\"Filter\": \"@Exception is not null\"", output);
            Assert.Contains("\"Columns\": [{\"Expression\": \"Column1\"}, {\"Expression\": \"round(Property2, 1)\"}]", output);

            return Task.CompletedTask;
        }
    }
}
