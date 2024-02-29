using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Forward;

#if WINDOWS

[Command("forward", "status", "Show the status of the Seq Forwarder service",
    Example = "seqcli forward status")]
class StatusCommand : Command
{
    public StatusCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
    }
}

#endif