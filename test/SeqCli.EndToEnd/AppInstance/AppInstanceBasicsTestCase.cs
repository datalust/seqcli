using System;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.AppInstance;

public class AppInstanceBasicsTestCase : ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var exit = runner.Exec("appinstance list", "-i appinstance-none");
        Assert.Equal(1, exit);

        exit = runner.Exec("app install", "--package-id Seq.App.EmailPlus");
        Assert.Equal(0, exit);

        var app = (await connection.Apps.ListAsync()).Single();

        var title = Guid.NewGuid().ToString("N");
        exit = runner.Exec("appinstance create", $"-t {title} --app {app.Id} --stream -p To=example@example.com -p From=example@example.com -p Host=localhost");
        Assert.Equal(0, exit);

        var appInstance = (await connection.AppInstances.ListAsync()).Single();
        
        exit = runner.Exec("appinstance list", $"-t {title}");
        Assert.Equal(0, exit);

        var output = runner.LastRunProcess?.Output;
        Assert.StartsWith(appInstance.Id, output?.Trim());
        Assert.EndsWith(appInstance.Title, output?.Trim());

        exit = runner.Exec("appinstance remove", $"-t {title}");
        Assert.Equal(0, exit);

        exit = runner.Exec("appinstance list", $"-i {appInstance.Id}");
        Assert.Equal(1, exit);

        exit = runner.Exec("appinstance list", $"-t {title}");
        Assert.Equal(0, exit);

        var streamSignal = "signal-m33303,(signal-m33301~signal-m33302)";
        exit = runner.Exec("appinstance create", $"-t {Guid.NewGuid():N} --app {app.Id} --stream=\"{streamSignal}\" -p To=example@example.com -p From=example@example.com -p Host=localhost");
        Assert.Equal(0, exit);
    }
}
