using System;

namespace SeqCli.PlainText.Extraction
{
    [AttributeUsage(AttributeTargets.Property)]
    class MatcherAttribute : Attribute
    {
        public string Name { get; }

        public MatcherAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}