using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

#nullable enable

namespace SeqCli.EndToEnd.Version;

public class VersionCommandTestCase : ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var exit = runner.Exec("version", disconnected: true);
        Assert.Equal(0, exit);
            
        var version = runner.LastRunProcess!.Output;

        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.NotEqual("1.0.0", version);
        Assert.NotEqual("0.0.0", version);
        
        // Loose version string to make sure the version appears reasonable
        Assert.Matches(@"[0-9]*\.[0-9]*\.[0-9]*(\+\S*)?", version);

        return Task.CompletedTask;
    }
}
