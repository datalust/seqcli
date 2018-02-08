namespace SeqCli.Cli.Features
{
    class FileInputFeature : CommandFeature
    {
        public string InputFilename { get; private set; }

        public override void Enable(OptionSet options)
        {
            options.Add("i=|input=",
                "CLEF file to ingest; if not specified, `STDIN` will be used",
                v => InputFilename = string.IsNullOrWhiteSpace(v) ? null : v.Trim());
        }
    }
}
