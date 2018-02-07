namespace SeqCli.Cli
{
    public class CommandMetadata : ICommandMetadata
    {
        public string Name { get; set; }
        public string HelpText { get; set; }
        public string Example { get; set; }
    }
}
