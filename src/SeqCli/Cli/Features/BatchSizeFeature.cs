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
            options.Add("batchsize=",
                "The number of events in each batch to send to Seq.",
                v => _value = int.Parse(ArgumentString.Normalize(v)));
        }
    }
}
