namespace SeqCli.Cli.Features;

class ListenUriFeature : CommandFeature
{
    public string? ListenUri { get; private set; }

    public override void Enable(OptionSet options)
    {
        options.Add("l=|listen=",
            "Set the address `seqcli forwarder` will listen at; http://127.0.0.1:15341/ is used by default.",
            v => ListenUri = v);
    }
}