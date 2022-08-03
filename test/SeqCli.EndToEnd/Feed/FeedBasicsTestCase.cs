using System.Threading.Tasks;
using System.Linq;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Feed
{
    // ReSharper disable once UnusedType.Global
    public class FeedBasicsTestCase : ICliTestCase
    {
        public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
        {
            var exit = runner.Exec("feed list", "-n nuget.org");
            Assert.Equal(0, exit);

            exit = runner.Exec("feed list", "-i feed-missing");
            Assert.Equal(1, exit);

            AssertExistence(runner, "Example", false);

            exit = runner.Exec("feed create", "-n Example -l \"https://example.com\" -u Test -p Pass");
            Assert.Equal(0, exit);
            
            AssertExistence(runner, "Example", true);
 
            var feed = (await connection.Feeds.ListAsync()).Single(f => f.Name == "Example");
            Assert.Equal("Example", feed.Name);
            Assert.Equal("https://example.com", feed.Location);
            Assert.Equal("Test", feed.Username);
            Assert.Null(feed.NewPassword);

            exit = runner.Exec("feed remove", "-n Example");
            Assert.Equal(0, exit);
            
            AssertExistence(runner, "Example", false);
        }

        static void AssertExistence(CliCommandRunner runner, string feedName, bool exists)
        {
            var exit = runner.Exec("feed list", $"-n {feedName}");
            Assert.Equal(0, exit);

            var output = runner.LastRunProcess!.Output;

            if (exists)
            {
                Assert.Contains(feedName, output);
            }
            else
            {
                Assert.Equal("", output.Trim());
            }
        }
    }
}