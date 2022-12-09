using System;
using SeqCli.Util;

namespace SeqCli.Cli.Features
{
    class BatchSizeFeature: CommandFeature
    {
        const int DefaultBatchSize = 100;
        int? _value;

        public int Value => _value ?? DefaultBatchSize;

        public override void Enable(OptionSet options)
        {
            options.Add("batch-size=",
                $"The maximum number of events to send in each request to the ingestion endpoint; if not specified a value of `{DefaultBatchSize}` will be used",
                v => _value = int.Parse(ArgumentString.Normalize(v) ?? throw new ArgumentException("Batch size requires an argument.")));
        }
    }
}
