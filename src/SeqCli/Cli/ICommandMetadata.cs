namespace SeqCli.Cli
{
    interface ICommandMetadata
    {
        string Name { get; }
        string HelpText { get; }
    }
}
