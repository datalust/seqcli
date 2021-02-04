using System.Threading.Tasks;
using Serilog;

namespace SeqCli.Sample.Loader
{
    static class Simulation
    {
        public static async Task RunAsync()
        {
            using var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Origin", "seqcli sample ingest")
                .WriteTo.Console()
                .CreateLogger();

            await Roastery.Program.Main(logger);
        }
    }
}