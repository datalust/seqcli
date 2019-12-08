using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

#if MULTIUSER_TESTS
using System.IO;
#endif

namespace SeqCli.EndToEnd.User
{
    public class UserBasicsTestCase : ICliTestCase
    {
        public Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var exit = runner.Exec("user list");
            Assert.Equal(0, exit);

            exit = runner.Exec("user list", "-n admin");
            Assert.Equal(0, exit);

            var output = runner.LastRunProcess.Output;
            Assert.Equal("user-admin admin", output.Trim());

#if MULTIUSER_TESTS
            exit = runner.Exec("user create", "-n alice -d \"Alice Example\" -r \"User (read/write)\" -p test");
            Assert.Equal(0, exit);

            var lines = new StringReader(runner.LastRunProcess.Output);
            lines.ReadLine(); // Skip password STDIN warning
            var id = lines.ReadLine()?.Split()[0];
            Assert.NotNull(id);

            exit = runner.Exec("user remove", $"-i {id}");
            Assert.Equal(0, exit);

            exit = runner.Exec("user list", "-n alice");
            Assert.Equal(1, exit);
#endif

            return Task.CompletedTask;
        }
    }
}
