using System;

namespace SeqCli.Cli;

[Flags]
public enum SupportedPlatforms
{
    None,
    Windows,
    Unix,
    All = Windows | Unix
}