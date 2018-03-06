using System.Threading.Tasks;
using Serilog.Events;

namespace SeqCli.Ingestion
{
    interface ILogEventReader
    {
        Task<ReadResult> TryReadAsync();
    }
}
