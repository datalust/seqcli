using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.License
{
    public class LicenseApplyTestCase : ICliTestCase
    {
        readonly TestDataFolder _testDataFolder;

        public LicenseApplyTestCase(TestDataFolder testDataFolder)
        {
            _testDataFolder = testDataFolder;
        }
        
        public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
        {
            var filename = _testDataFolder.ItemPath("license.txt");
            File.WriteAllText(filename, "Ceci n'est pas une licence");
            runner.Exec("license apply", $"--certificate=\"{filename}\"");
            Assert.Equal(
                "The command failed: 400 - The license format is invalid: data precedes any keys.",
                runner.LastRunProcess.Output.Trim());
            return Task.CompletedTask;
        }
    }
}