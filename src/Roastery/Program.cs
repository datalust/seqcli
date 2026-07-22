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
using Roastery.Model;
using Roastery.Util;
using Roastery.Web;
using Serilog;

namespace Roastery;

// Named this way to make stack traces a little more believable :-)
public static class Program
{
    public static async Task Main(ILogger logger, PropertyNameMapping propertyNameMapping,
        CancellationToken cancellationToken = default)
    {
        var webMetrics = new RoasteryWebMetrics();
        var productionMetrics = new RoasteryProductionMetrics();

        var webApplicationLogger = logger.ForContext("Application", "Roastery Web Frontend");
        var productionLogger = logger.ForContext("Application", "Roastery Production");

        // Sample metrics
        var sampleWebMetrics = webMetrics.PeriodicSample(TimeSpan.FromSeconds(5), (timestamp, sample, ct) =>
        {
            foreach (var evt in sample.ToLogEvents(webApplicationLogger, propertyNameMapping, timestamp))
            {
                webApplicationLogger.Write(evt);
            }

            return Task.CompletedTask;
        }, cancellationToken);

        var sampleProductionMetrics = productionMetrics.PeriodicSample(TimeSpan.FromSeconds(5),
            (timestamp, sample, ct) =>
            {
                foreach (var evt in sample.ToLogEvents(productionLogger, propertyNameMapping, timestamp))
                {
                    productionLogger.Write(evt);
                }

                return Task.CompletedTask;
            }, cancellationToken);

        var database = new Database(webApplicationLogger, "roastery");
        DatabaseMigrator.Populate(database);

        var loadingDock = new LoadingDock();
        var productionSchedule = new ProductionSchedule();

        // Schedule the first outage for 2 hours into ingestion
        var maintenanceSchedule = new MaintenanceSchedule(DateTime.UtcNow + TimeSpan.FromHours(Distribution.Uniform(2, 7)));

        var client = new HttpClient(
            "https://roastery.datalust.co",
            new NetworkLatencyMiddleware(
                new RequestLoggingMiddleware(webApplicationLogger, webMetrics,
                    new SchedulingLatencyMiddleware(
                        new FaultInjectionMiddleware(webApplicationLogger,
                            new Router([
                                new OrdersController(logger, webMetrics, database),
                                new ProductsController(logger, webMetrics, database),
                                new InventoryController(logger, webMetrics, database)
                            ], webApplicationLogger))))));

        var agents = new List<Agent>();

        for (var i = 0; i < 100; ++i)
            agents.Add(new Customer(client, Person.Generate(), (int)Distribution.Uniform(60000, 180000)));

        for (var i = 0; i < 3; ++i)
            agents.Add(new WarehouseStaff(client, loadingDock, productionSchedule));

        var rng = new Random();
        for (var i = 1; i <= 4; ++i)
            agents.Add(new RoastingMachine(productionLogger, productionMetrics, loadingDock, productionSchedule,
                maintenanceSchedule, $"roaster-{rng.Next():x8}"));

        agents.Add(new FacilitySensors(productionLogger, productionMetrics));

        var batchApplicationLogger = logger.ForContext("Application", "Roastery Batch Processing");
        agents.Add(new CatalogBatch(client, batchApplicationLogger));
        agents.Add(new ArchivingBatch(client, batchApplicationLogger));

        await Task.WhenAll(agents.Select(a => Agent.Run(a, cancellationToken)));
        await Task.WhenAll(sampleWebMetrics, sampleProductionMetrics);
    }
}