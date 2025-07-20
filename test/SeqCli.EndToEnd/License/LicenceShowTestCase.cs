using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.License;

public class LicenseShowTestCase : ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var license = await connection.Licenses.FindCurrentAsync();
        Assert.True(license.IsSingleUser);
        
        runner.Exec("license show");
        Assert.Equal(
            "",
            runner.LastRunProcess!.Output.Trim());

        runner.Exec("license show --json");
        Assert.Contains(
            "You're using the free Individual license.",
            runner.LastRunProcess!.Output.Trim());
    }
}