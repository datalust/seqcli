using System.Threading.Tasks;
using Seq.Api;
using SeqCli.Ingestion;
using Serilog;

namespace SeqCli.Sample.Loader
{
    static class Simulation
    {
        public static async Task RunAsync(SeqConnection connection, string apiKey)
        {
            var reader = new BufferingSink();
            
            using var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Origin", "seqcli sample ingest")
                .WriteTo.Console()
                .WriteTo.Sink(reader)
                .CreateLogger();

            var ship = Task.Run(() => LogShipper.ShipEvents(connection, apiKey, reader,
                InvalidDataHandling.Fail, SendFailureHandling.Continue));

            await Roastery.Program.Main(logger);
            logger.Dispose();
            await ship;
        }
    }
}