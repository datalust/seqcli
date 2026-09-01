using System.Threading.Tasks;
using SeqCli.Ingestion;

namespace SeqCli.Tests.Support;

class FixedEventReader : IEventReader
{
    readonly ReadResult _result;

    public FixedEventReader(ReadResult result)
    {
        _result = result;
    }

    public Task<ReadResult> TryReadAsync()
    {
        return Task.FromResult(_result);
    }
}