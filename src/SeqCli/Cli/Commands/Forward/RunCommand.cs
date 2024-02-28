using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Forward;

[Command("forward", "run", "Run the Seq Forwarder server interactively",
    Example = "seqcli forward run")]
class RunCommand : Command
{
    public RunCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
    }
}