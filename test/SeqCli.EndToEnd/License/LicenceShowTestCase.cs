using System;
using System.IO;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.License;

public class LicenseShowTestCase : ICliTestCase
{
    readonly TestDataFolder _testDataFolder;

    public LicenseShowTestCase(TestDataFolder testDataFolder)
    {
        _testDataFolder = testDataFolder;
    }

    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {

        // test empty text output if no license is applied
        runner.Exec("license show");
        Assert.Equal(
            "",
            runner.LastRunProcess.Output.Trim());

        // test json output if no license is applied
        runner.Exec("license show --json");
        Assert.Contains(
            "You're using the free Individual license.",
            runner.LastRunProcess.Output.Trim());
        return Task.CompletedTask;
    }
}