using System.Threading.Tasks;
using ModelContextProtocol.Client;
using Seq.Api;
using Seq.Api.Model.Settings;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Mcp;

// ReSharper disable once UnusedType.Global
public class McpInstanceTitleTestCase : ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        const string instanceTitle = "Test Instance";

        var setting = await connection.Settings.FindNamedAsync(SettingName.InstanceTitle);
        setting.Value = instanceTitle;
        await connection.Settings.UpdateAsync(setting);

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "seqcli mcp run",
            Command = "dotnet",
            Arguments = [TestConfiguration.TestedBinary, "mcp", "run", $"--server={connection.Client.ServerUrl}"]
        });

        await using var client = await McpClient.CreateAsync(transport);

        Assert.EndsWith($" ({instanceTitle})", client.ServerInfo.Name);
        Assert.Equal($"Connected to Seq ({instanceTitle})", client.ServerInstructions);
    }
}
