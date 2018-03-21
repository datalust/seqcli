using System.Threading.Tasks;
using Seq.Api;
using Serilog;

namespace SeqCli.EndToEnd.Support
{
    interface ICliTestCase
    {
        Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner);
    }
}
