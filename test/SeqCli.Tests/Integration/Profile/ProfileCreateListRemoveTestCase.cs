using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Tests.Integration.Support;
using Serilog;
using Xunit;

namespace SeqCli.Tests.Integration.Profile
{
    [Collection("Integration")]
    public class ProfileCreateListRemoveTestCase : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public ProfileCreateListRemoveTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }

        [Fact]
        public Task ExecuteAsync()
        {
            var create = _cliTestCase._runner.Exec("profile create", "-n Test -s https://seq.example.com -a 123", disconnected: true);
            Assert.Equal(0, create);

            Assert.Equal(0, _cliTestCase._runner.Exec("profile list", disconnected: true));
            Assert.Contains("test (https://seq.example.com)", _cliTestCase._runner.LastRunProcess.Output);

            Assert.Equal(0, _cliTestCase._runner.Exec("config", disconnected: true));
            Assert.Contains("profiles[test].serverUrl", _cliTestCase._runner.LastRunProcess.Output);
            Assert.Contains("https://seq.example.com", _cliTestCase._runner.LastRunProcess.Output);
            Assert.Contains("profiles[test].apiKey", _cliTestCase._runner.LastRunProcess.Output);
            Assert.Contains("123", _cliTestCase._runner.LastRunProcess.Output);

            var remove = _cliTestCase._runner.Exec("profile remove", "-n Test", disconnected: true);
            Assert.Equal(0, remove);

            return Task.CompletedTask;
        }
    }
}