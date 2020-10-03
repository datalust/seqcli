using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Tests.Integration.Support;
using Serilog;
using Xunit;

namespace SeqCli.Tests.Integration.ApiKey
{
    [Collection("Integration")]
    public class ApiKeyCreateTestCase : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public ApiKeyCreateTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }

        [Fact]
        public Task ExecuteAsync()
        {
            var exit = _cliTestCase._runner.Exec("apikey create", "-t Test");
            Assert.Equal(0, exit);

            exit = _cliTestCase._runner.Exec("apikey list", "-t Test --json --no-color");
            Assert.Equal(0, exit);

            var output = _cliTestCase._runner.LastRunProcess.Output;
            Assert.Contains("\"AssignedPermissions\": [\"Ingest\"]", output);

            return Task.CompletedTask;
        }
    }
}