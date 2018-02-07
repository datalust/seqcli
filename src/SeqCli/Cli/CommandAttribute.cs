using System;

namespace SeqCli.Cli
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute, ICommandMetadata
    {
        public string Name { get; }
        public string HelpText { get; }

        public string Example { get; set; }

        public CommandAttribute(string name, string helpText)
        {
            Name = name;
            HelpText = helpText;
        }
    }
}
