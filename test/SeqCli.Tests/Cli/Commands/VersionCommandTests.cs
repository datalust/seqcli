#if !DEBUG

using SeqCli.Cli.Commands;
using Xunit;

namespace SeqCli.Tests.Cli.Commands
{
    public class VersionCommandTests
    {
        [Fact]
        public void VersionIsSet()
        {
            var version = VersionCommand.GetVersion();
            Assert.False(string.IsNullOrEmpty(version));
            Assert.NotEqual("1.0.0", version);
            Assert.NotEqual("0.0.0", version);
        }
    }
}
#endif
