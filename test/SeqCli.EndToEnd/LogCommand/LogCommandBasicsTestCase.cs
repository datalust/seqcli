using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.LogCommand
{
    public class LogCommandBasicsTestCase : ICliTestCase
    {
        public async Task ExecuteAsync(
            SeqConnection connection,
            ILogger logger,
            CliCommandRunner runner)
        {
            var exit = runner.Exec("log", "-m \"Hello, {Name}!\" -p Name=world");
            Assert.Equal(0, exit);

            var events = await connection.Events.ListAsync(render: true, filter: "Name = 'world'");
            Assert.Single(events);

            var hello = events.Single();
            Assert.Equal("Hello, world!", hello.RenderedMessage);
        }
    }
}