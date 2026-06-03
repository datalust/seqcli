using System.Threading.Tasks;
using JetBrains.Annotations;
using Seq.Api;
using Serilog;

namespace SeqCli.EndToEnd.Support;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
interface ICliTestCase
{
    Task ExecuteAsync(SeqConnection connection, ILogger logger, CliCommandRunner runner);
}
