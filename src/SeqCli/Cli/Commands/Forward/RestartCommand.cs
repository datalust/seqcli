using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Forward;

[Command("forward", "restart", "Restart the Seq Forwarder Windows service",
    Example = "seqcli forward restart")]
class RestartCommand : Command
{
    public RestartCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
    }
}