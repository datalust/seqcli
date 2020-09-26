using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Setting
{
    public class SettingUpdateGetTestCase : ICliTestCase
    {
        public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
        {
            int exit = 0;
            
            exit = runner.Exec("setting update", "-n InstanceTitle -v TestInstance");
            Assert.Equal(0, exit);

            exit = runner.Exec("setting find", "-n InstanceTitle");
            Assert.Equal(0, exit);

            var output = runner.LastRunProcess.Output;

            return Task.CompletedTask;
        }
    }
}