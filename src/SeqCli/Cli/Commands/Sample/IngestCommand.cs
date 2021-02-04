using System.Threading.Tasks;
using SeqCli.Sample.Loader;

namespace SeqCli.Cli.Commands.Sample
{
    [Command("sample", "ingest", "Log sample events into a Seq instance",
        Example = "seqcli sample ingest")]
    class IngestCommand : Command
    {
        protected override async Task<int> Run()
        {
            await new Simulation().RunAsync();
            return 0;
        }
    }
}