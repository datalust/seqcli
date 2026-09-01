using System.Threading.Tasks;

namespace SeqCli.Ingestion;

interface IEventReader
{
    Task<ReadResult> TryReadAsync();
}