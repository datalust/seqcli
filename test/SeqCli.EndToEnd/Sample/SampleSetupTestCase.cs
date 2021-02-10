using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Sample
{
    public class SampleSetupTestCase : ICliTestCase
    {
        public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
        {
            var exit = runner.Exec("sample setup", "--confirm");
            Assert.Equal(0, exit);

            // Depends on the most other entities, so is a pretty good proxy for success.
            var sampleWorkspace = (await connection.Workspaces.ListAsync(shared: true))
                .SingleOrDefault(w => w.Title == "Sample");

            Assert.NotNull(sampleWorkspace);
        }
    }
}