using System;
using System.Threading.Tasks;
using SeqCli.Api;
using SeqCli.Cli.Features;
using SeqCli.Config;

namespace SeqCli.Cli.Commands.Diagnostics;

[Command("diagnostics", "ingestionlog", "Retrieve the ingestion log",
    Example = "seqcli diagnostics ingestionlog")]
class IngestionLogCommand : Command
{
    readonly ConnectionFeature _connection;
    readonly StoragePathFeature _storagePath;

    public IngestionLogCommand()
    {
        _connection = Enable<ConnectionFeature>();
        _storagePath = Enable<StoragePathFeature>();
    }

    protected override async Task<int> Run()
    {
        var config = RuntimeConfigurationLoader.Load(_storagePath);
        var connection = SeqConnectionFactory.Connect(_connection, config);

        var ingestionLog = await connection.Diagnostics.GetIngestionLogAsync();
        Console.WriteLine(ingestionLog);

        return 0;
    }
}
