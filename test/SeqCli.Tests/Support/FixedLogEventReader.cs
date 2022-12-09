using System.Threading.Tasks;
using SeqCli.Ingestion;

namespace SeqCli.Tests.Support;

class FixedLogEventReader : ILogEventReader
{
    readonly ReadResult _result;

    public FixedLogEventReader(ReadResult result)
    {
        _result = result;
    }

    public Task<ReadResult> TryReadAsync()
    {
        return Task.FromResult(_result);
    }
}