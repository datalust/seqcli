using System.Collections.Generic;
using System.IO;
using Seq.Apps;
using SeqCli.Apps.Hosting;
using SeqCli.Tests.Support;
using Serilog.Core;
using Xunit;

namespace SeqCli.Tests.Apps;

public class AppContainerTests
{
    [Fact]
    public void CanLoadOutputApp()
    {
        const string appBinaries = "Apps/FirstOfTypeBinaries";
        Assert.True(Directory.Exists(appBinaries));

        var appContainer = new AppContainer(Logger.None, appBinaries,
            new App(Some.String(), Some.String(), new Dictionary<string, string>(), "./storage"),
            new Host(Some.UriString(), null));

        appContainer.Dispose();
    }

    [Fact]
    public void CanLoadInputApp()
    {
        const string appBinaries = "Apps/HealthCheckBinaries";
        Assert.True(Directory.Exists(appBinaries));

        var settings = new Dictionary<string, string> { ["TargetUrl"] = Some.UriString() };

        var appContainer = new AppContainer(Logger.None, appBinaries,
            new App(Some.String(), Some.String(), settings, "./storage"),
            new Host(Some.UriString(), null));

        appContainer.Dispose();
    }
}