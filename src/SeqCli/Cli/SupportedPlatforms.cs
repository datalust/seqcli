using System;

namespace SeqCli.Cli;

[Flags]
public enum SupportedPlatforms
{
    None,
    Windows,
    Posix,
    All = Windows | Posix
}