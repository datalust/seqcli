using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Tests.Integration.Support;
using Serilog;
using Xunit;

#if MULTIUSER_TESTS
using System.IO;
#endif

namespace SeqCli.Tests.Integration.User
{
    [Collection("Integration")]
    public class UserBasicsTestCase : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public UserBasicsTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }

        [Fact]
        public Task ExecuteAsync()
        {
            var exit = _cliTestCase._runner.Exec("user list");
            Assert.Equal(0, exit);

            exit = _cliTestCase._runner.Exec("user list", "-n admin");
            Assert.Equal(0, exit);

            var output = _cliTestCase._runner.LastRunProcess.Output;
            Assert.Equal("user-admin admin", output.Trim());

#if MULTIUSER_TESTS
            exit = _cliTestCase.runner.Exec("user create", "-n alice -d \"Alice Example\" -r \"User (read/write)\" -p test");
            Assert.Equal(0, exit);

            var lines = new StringReader(runner.LastRunProcess.Output);
            lines.ReadLine(); // Skip password STDIN warning
            var id = lines.ReadLine()?.Split()[0];
            Assert.NotNull(id);

            exit = _cliTestCase.runner.Exec("user remove", $"-i {id}");
            Assert.Equal(0, exit);

            exit = _cliTestCase.runner.Exec("user list", "-n alice");
            Assert.Equal(1, exit);
#endif

            return Task.CompletedTask;
        }
    }
}
