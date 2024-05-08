using System;
using System.Threading.Tasks;
using Seq.Api;
using SeqCli.EndToEnd.Support;
using Serilog;
using Xunit;

#nullable enable

namespace SeqCli.EndToEnd.Help;

public class MarkdownHelpTestCase : ICliTestCase
{
    public Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner)
    {
        var exit = runner.Exec("help", "--markdown", disconnected: true);
        Assert.Equal(0, exit);
            
        var markdown = runner.LastRunProcess!.Output;

        Assert.StartsWith("## Commands", markdown);

        var indexOfTemplateExport = markdown.IndexOf("### `template export`", StringComparison.Ordinal);
        var indexOfTemplateImport = markdown.IndexOf("### `template import`", StringComparison.Ordinal);
        Assert.NotEqual(indexOfTemplateExport, indexOfTemplateImport);
        Assert.True(indexOfTemplateExport < indexOfTemplateImport);
        
        Assert.DoesNotContain("### `forwarder run`", markdown);

        return Task.CompletedTask;
    }
}