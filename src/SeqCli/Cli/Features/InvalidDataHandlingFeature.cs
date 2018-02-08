using System;
using SeqCli.Ingestion;

namespace SeqCli.Cli.Features
{
    class InvalidDataHandlingFeature : CommandFeature
    {
        public InvalidDataHandling InvalidDataHandling { get; private set; }

        public override void Enable(OptionSet options)
        {
            options.Add("invalid-data=",
                "Specify how invalid data is handled: fail (default) or ignore",
                v => InvalidDataHandling = (InvalidDataHandling)Enum.Parse(typeof(InvalidDataHandling), v, ignoreCase: true));
        }
    }

}
