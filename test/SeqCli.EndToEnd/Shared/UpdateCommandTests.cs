using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

namespace SeqCli.EndToEnd.Shared;

class UpdateCommandTests(TestConfiguration configuration): ICliTestCase
{
    public async Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        // Ensure there's at least one API key...
        var apiKey = await connection.ApiKeys.TemplateAsync();
        apiKey.Title = "Test";
        await connection.ApiKeys.AddAsync(apiKey);
        
        var exit = runner.Exec("app install", "--package-id Seq.App.EmailPlus");
        Assert.Equal(0, exit);

        // One app instance...
        var app = (await connection.Apps.ListAsync()).Single();

        var title = Guid.NewGuid().ToString("N");
        exit = runner.Exec("appinstance create", $"-t {title} --app {app.Id} --stream -p To=example@example.com -p From=example@example.com -p Host=localhost");
        Assert.Equal(0, exit);

        // One retention policy...
        var retentionPolicy = await connection.RetentionPolicies.TemplateAsync();
        retentionPolicy.RetentionTime = TimeSpan.FromDays(100);
        await connection.RetentionPolicies.AddAsync(retentionPolicy);
        
        // One workspace...
        var workspace = await connection.Workspaces.TemplateAsync();
        workspace.OwnerId = null;
        await connection.Workspaces.AddAsync(workspace);
        
        foreach (var commandGroup in new[]
                 {
                     "apikey",
                     "appinstance",
                     "feed",
                     "retention",
                     "signal",
                     "user",
                     "workspace"
                 })
        {
            try
            {
                ListFirstThenUpdate(runner, commandGroup);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed in `{commandGroup}` command group.", ex);
            }
        }
    }

    void ListFirstThenUpdate(CliCommandRunner runner, string commandGroup)
    {
        var exit = runner.Exec($"{commandGroup} list", "--json");
        Assert.Equal(0, exit);
        
        var json = new StringReader(runner.LastRunProcess!.Output).ReadLine()?.Trim();
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
        
        using var process = configuration.SpawnCliProcess($"{commandGroup} update", "--json-stdin", supplyInput: true);
        process.WriteLineStdin(json);
        process.CompleteStdin();
        
        exit = process.WaitForExit(CliCommandRunner.DefaultExecTimeout);
        Assert.Equal(0, exit);
    }
}
