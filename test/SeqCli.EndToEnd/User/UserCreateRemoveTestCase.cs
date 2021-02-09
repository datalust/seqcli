using System;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;
using System.IO;
using System.Linq;

namespace SeqCli.EndToEnd.User
{
    [CliTestCase(Multiuser = true)]
    public class UserCreateRemoveTestCase : ICliTestCase
    {
        public async Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var exit = runner.Exec(
                "user create",
                "-n alice -d \"Alice Example\" -r \"User (read/write)\" -p test@1234");
            Assert.Equal(0, exit);

            var id = runner.LastRunProcess.Output.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            Assert.NotNull(id);

            var user = await connection.Users.FindAsync(id);
            Assert.All(user.RoleIds, r => r.StartsWith("role-", StringComparison.OrdinalIgnoreCase));
            
            exit = runner.Exec("user remove", $"-i {id}");
            Assert.Equal(0, exit);

            exit = runner.Exec("user list", "-i {id}");
            Assert.Equal(1, exit);
        }
    }
}
