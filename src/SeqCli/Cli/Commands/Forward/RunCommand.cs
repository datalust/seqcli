using System;
using SeqCli.Cli.Features;
using SeqCli.Config;
using SeqCli.Connection;

namespace SeqCli.Cli.Commands.Forward;

[Command("forward", "run", "Run the Seq Forwarder server interactively",
    Example = "seqcli forward run")]
class RunCommand : Command
{
#pragma warning disable CS0414 // Field is assigned but its value is never used
    bool _noLogo;
#pragma warning restore CS0414 // Field is assigned but its value is never used
    readonly ListenUriFeature _listenUri;

    public RunCommand(SeqConnectionFactory connectionFactory, SeqCliConfig config)
    {
        Options.Add("nologo", _ => _noLogo = true);
        _listenUri = Enable<ListenUriFeature>();
    }
}