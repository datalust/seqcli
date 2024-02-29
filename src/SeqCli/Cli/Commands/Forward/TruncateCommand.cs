using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Forward;

[Command("forward", "truncate", "Clear the log buffer contents",
    Example = "seqcli forward truncate")]
class TruncateCommand : Command
{
    public TruncateCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
    }
}