namespace SeqCli.Cli.Features;

class ListenUriFeature : CommandFeature
{
    public string? ListenUri { get; private set; }

    public override void Enable(OptionSet options)
    {
        options.Add("l=|listen=",
            "Set the listen Uri; http://localhost:15341/ is used by default.",
            v => ListenUri = v);
    }
}