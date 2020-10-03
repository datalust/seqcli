using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Tests.Integration.Support;
using Serilog;
using Xunit;

namespace SeqCli.Tests.Integration.Ingest
{
    [Collection("Integration")]
    public class SerilogTextIngestionTestCase : IClassFixture<CliTestCase> 
    {
        private readonly CliTestCase _cliTestCase;

        public SerilogTextIngestionTestCase(CliTestCase cliTestCase)
        {
            _cliTestCase = cliTestCase;
        }
        
        [Fact]
        public async Task ExecuteAsync()
        {
            var inputFile = Path.Combine("Integration", "Data", "serilog-events.txt");
            Assert.True(File.Exists(inputFile));

            var exit = _cliTestCase._runner.Exec("ingest", $"-i \"{inputFile}\"" + " -x \"{@t:timestamp} [{@l:level}] {@m:*}{:n}{@x:*}\"");
            Assert.Equal(0, exit);

            var events = await _cliTestCase._connection.Events.ListAsync();
            Assert.Equal(3, events.Count);
            // Since @l is a level, mapping to full/non-abbreviated names is performed
            Assert.Equal(1, events.Count(e => e.Level == "Error"));
            Assert.Equal(2, events.Count(e => e.Level == "Information"));
            Assert.Equal(1, events.Count(e => e.Exception != null && e.Exception.Contains("MyFlakyMethod()")));
        }
    }
}
