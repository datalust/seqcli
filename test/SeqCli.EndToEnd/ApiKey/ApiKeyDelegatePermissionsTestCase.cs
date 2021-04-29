using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.ApiKey
{
    [CliTestCase(Multiuser = true)]
    public class ApiKeyDelegatePermissionsTestCase : ICliTestCase
    {
        public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
        {
            var exit = runner.Exec(
                "user create",
                "-n carol -r \"Administrator\" -p test@1234 --no-password-change");
            Assert.Equal(0, exit);

            exit = runner.Exec(
                "apikey create",
                "-t Setup --permissions=Setup,Write --connect-username=carol --connect-password=\"test@1234\"");
            Assert.Equal(0, exit);

            exit = runner.Exec("apikey list", "-t Setup --json --no-color");
            Assert.Equal(0, exit);

            var output = runner.LastRunProcess.Output;
            Assert.Contains("\"AssignedPermissions\": [\"Setup\", \"Write\"]", output);

            return Task.CompletedTask;
        }
    }
}