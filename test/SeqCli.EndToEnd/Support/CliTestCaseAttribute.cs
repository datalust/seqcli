using System;

namespace SeqCli.EndToEnd.Support
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CliTestCaseAttribute : Attribute
    {
        public bool Multiuser { get; set; }
    }
}