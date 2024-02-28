using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Forward;

[Command("forward", "dump", "Print the complete log buffer contents as JSON",
    Example = "seqcli forward dump")]
class DumpCommand : Command
{
    public DumpCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
    }
}