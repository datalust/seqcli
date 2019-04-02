using System.Threading.Tasks;

namespace SeqCli.Ingestion
{
    interface ILogEventReader
    {
        Task<ReadResult> TryReadAsync();
    }
}
