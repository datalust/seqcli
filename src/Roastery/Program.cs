using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Agents;
using Roastery.Api;
using Roastery.Data;
using Roastery.Fake;
using Roastery.Metrics;
using Roastery.Util;
using Roastery.Web;
using Serilog;

namespace Roastery;

// Named this way to make stack traces a little more believable :-)
public static class Program
{
    public static async Task Main(ILogger logger, PropertyNameMapping propertyNameMapping, CancellationToken cancellationToken = default)
    {
        var metrics = new RoasteryMetrics();
        
        var webApplicationLogger = logger.ForContext("Application", "Roastery Web Frontend");

        // Sample metrics
        var periodicSample = RoasteryMetrics.PeriodicSample(metrics, TimeSpan.FromSeconds(5), (timestamp, sample, ct) =>
        {
            foreach (var evt in sample.ToLogEvents(webApplicationLogger, propertyNameMapping, timestamp))
            {
                webApplicationLogger.Write(evt);
            }
            
            return Task.CompletedTask;
        }, cancellationToken);

        var database = new Database(webApplicationLogger, "roastery");
        DatabaseMigrator.Populate(database);

        var client = new HttpClient(
            "https://roastery.datalust.co",
            new NetworkLatencyMiddleware(
                new RequestLoggingMiddleware(webApplicationLogger, metrics,
                    new SchedulingLatencyMiddleware(
                        new FaultInjectionMiddleware(webApplicationLogger,
                            new Router([
                                new OrdersController(logger, database),
                                new ProductsController(logger, database)
                            ], webApplicationLogger))))));

        var agents = new List<Agent>();

        for (var i = 0; i < 100; ++i)
            agents.Add(new Customer(client, Person.Generate(), (int)Distribution.Uniform(60000, 180000)));

        for (var i = 0; i < 3; ++i)
            agents.Add(new WarehouseStaff(client));

        var batchApplicationLogger = logger.ForContext("Application", "Roastery Batch Processing");
        agents.Add(new CatalogBatch(client, batchApplicationLogger));
        agents.Add(new ArchivingBatch(client, batchApplicationLogger));

        await Task.WhenAll(agents.Select(a => Agent.Run(a, cancellationToken)));
        await periodicSample;
    }
}