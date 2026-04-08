using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
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

    [Fact]
    public void RemovesInvalidTraceAndSpanIds()
    {
        var jo = new JObject
        {
            ["@tr"] = "A234567890123456A234567890123456",
            ["@sp"] = "A234567890123456",
            ["@ps"] = "A234567890123456"
        };
        Assert.NotEmpty(jo);
        AppContainer.SanitizeTraceIdentifiers(jo);
        Assert.Empty(jo);
    }

    [Fact]
    public void RemovesShortTraceAndSpanIds()
    {
        var jo = new JObject
        {
            ["@tr"] = "234567890123456234567890123456",
            ["@sp"] = "234567890123456",
            ["@ps"] = "234567890123456"
        };
        Assert.NotEmpty(jo);
        AppContainer.SanitizeTraceIdentifiers(jo);
        Assert.Empty(jo);
    }

    [Fact]
    public void RemovesZeroTraceAndSpanIds()
    {
        var jo = new JObject
        {
            ["@tr"] = "00000000000000000000000000000000",
            ["@sp"] = "0000000000000000",
            ["@ps"] = "0000000000000000"
        };
        Assert.NotEmpty(jo);
        AppContainer.SanitizeTraceIdentifiers(jo);
        Assert.Empty(jo);
    }

    [Fact]
    public void PreservesValidTraceAndSpanIds()
    {
        var jo = new JObject
        {
            ["@tr"] = "a234567890123456a234567890123456",
            ["@sp"] = "a234567890123456",
            ["@ps"] = "a234567890123456"
        };
        Assert.Equal(3, jo.Count);
        AppContainer.SanitizeTraceIdentifiers(jo);
        Assert.Equal(3, jo.Count);
    }
}