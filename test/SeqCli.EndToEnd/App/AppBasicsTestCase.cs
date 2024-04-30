using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.App;

// ReSharper disable once UnusedType.Global
public class AppBasicsTestCase: ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var exit = runner.Exec("feed list", "-n nuget.org");
        Assert.Equal(0, exit);

        exit = runner.Exec("app install", "--package-id Seq.App.EmailPlus");
        Assert.Equal(0, exit);

        exit = runner.Exec("app list", "--package-id Seq.App.EmailPlus");
        Assert.Equal(0, exit);

        exit = runner.Exec("app update", "--all");
        Assert.Equal(0, exit);

        exit = runner.Exec("app uninstall", "--package-id Seq.App.EmailPlus");
        Assert.Equal(0, exit);

        return Task.CompletedTask;
    }
}
