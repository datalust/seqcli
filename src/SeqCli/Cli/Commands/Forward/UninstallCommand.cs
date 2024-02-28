using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Forward;

[Command("forward", "uninstall", "Uninstall the Windows service",
    Example = "seqcli forward uninstall")]
internal class UninstallCommand : Command
{
    public UninstallCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
    }
}

