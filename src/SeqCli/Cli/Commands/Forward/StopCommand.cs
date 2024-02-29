using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Forward;

#if WINDOWS

[Command("forward", "stop", "Stop the Seq Forwarder service",
    Example = "seqcli forward stop")]
class StopCommand : Command
{
    public StopCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
    }
}

#endif