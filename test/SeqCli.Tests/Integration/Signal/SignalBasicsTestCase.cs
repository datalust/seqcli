using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Tests.Integration.Support;
using Serilog;
using Xunit;

namespace SeqCli.Tests.Integration.Signal
{
    [Collection("Integration")]
    public class SignalBasicsTestCase : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public SignalBasicsTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }

        [Fact]
        public Task ExecuteAsync()
        {
            var exit = _cliTestCase._runner.Exec("signal list", "-i signal-none");
            Assert.Equal(1, exit);

            exit = _cliTestCase._runner.Exec("signal list", "-t Warnings");
            Assert.Equal(0, exit);

            var output = _cliTestCase._runner.LastRunProcess.Output;
            Assert.Equal("signal-m33302 Warnings", output.Trim());

            exit = _cliTestCase._runner.Exec("signal remove", "-t Warnings");
            Assert.Equal(0, exit);

            exit = _cliTestCase._runner.Exec("signal list", "-i signal-m33302");
            Assert.Equal(1, exit);

            exit = _cliTestCase._runner.Exec("signal list", "-t Warnings");
            Assert.Equal(0, exit);

            return Task.CompletedTask;
        }
    }
}
