using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Tests.Integration.Support;
using Serilog;
using Xunit;

namespace SeqCli.Tests.Integration.Dashboard
{
    [Collection("Integration")]
    public class RenderTestCase : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public RenderTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }

        [Fact]
        public Task ExecuteAsync()
        {
            var exit = _cliTestCase._runner.Exec("dashboard list");
            Assert.Equal(0, exit);

            var id = _cliTestCase._runner.LastRunProcess.Output.Split(' ')[0];

            exit = _cliTestCase._runner.Exec("dashboard render", $"-i {id} -c \"All Events\" --last 1d --by 1h");
            Assert.Equal(0, exit);

            var lines = new StringReader(_cliTestCase._runner.LastRunProcess.Output);
            var firstLine = lines.ReadLine();
            Assert.Equal("\"time\",\"count\"", firstLine);

            return Task.CompletedTask;
        }
    }
}
