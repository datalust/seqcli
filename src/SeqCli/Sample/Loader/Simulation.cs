using System.Collections.Generic;
using System.Threading.Tasks;
using SeqCli.Sample.Loader.Api;
using SeqCli.Sample.Loader.Model;
using SeqCli.Sample.Loader.Web;
using Serilog;

namespace SeqCli.Sample.Loader
{
    public class Simulation
    {
        public async Task RunAsync()
        {
            using var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Origin", "seqcli sample ingest")
                .WriteTo.Console()
                .CreateLogger();

            var webApplicationLogger = logger.ForContext("Application", "SeqCafe.Web");
            
            var client = new HttpClient(
                "https://cafe.datalust.co",
                new NetworkLatencyMiddleware(
                    new RequestLoggingMiddleware(webApplicationLogger,
                        new SchedulingLatencyMiddleware(
                            new Router(new Controller[]
                            {
                                new ProductsController()
                            }, webApplicationLogger)))));

            var simulationClientLogger = logger.ForContext("Application", "SeqCafe.Client").ForContext<Simulation>();
            foreach (var product in await client.GetAsync<List<Product>>("api/products"))
            {
                simulationClientLogger.Information("Found product {@Product}", product);
            }
        }
    }
}