using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Forward;

#if Windows

[Command("forward", "install", "Install the Seq Forwarder as a Windows service",
    Example = "seqcli forward install")]
class InstallCommand : Command
{
    public InstallCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
    }
}

#endif