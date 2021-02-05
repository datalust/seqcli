using System;

namespace SeqCli.EndToEnd.Support
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CliTestCaseAttribute : Attribute
    {
        public bool IsSetup { get; set; }
        public bool Multiuser { get; set; }
    }
}