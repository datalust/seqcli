using System;

namespace SeqCli.Cli
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute, ICommandMetadata
    {
        public string Name { get; private set; }
        public string HelpText { get; private set; }

        public CommandAttribute(string name, string helpText)
        {
            Name = name;
            HelpText = helpText;
        }
    }
}
