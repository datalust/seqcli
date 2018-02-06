using System;
using System.Collections.Generic;

namespace SeqCli.Cli
{
    abstract class CommandFeature
    {
        public abstract void Enable(OptionSet options);

        public virtual IEnumerable<string> GetUsageErrors() => Array.Empty<string>();
    }
}
