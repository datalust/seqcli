using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Roastery.Api;
using Roastery.Data;
using Roastery.Model;
using Roastery.Web;
using Serilog;

namespace Roastery
{
    public static class Program
    {
        public static async Task Main(ILogger logger)
        {
            var webApplicationLogger = logger.ForContext("Application", "Roastery Web Frontend");

            var database = new Database(webApplicationLogger, "roastery");
            DatabaseMigrator.Populate(database);
            
            var client = new HttpClient(
                "https://roastery.datalust.co",
                new NetworkLatencyMiddleware(
                    new RequestLoggingMiddleware(webApplicationLogger,
                        new SchedulingLatencyMiddleware(
                            new FaultInjectionMiddleware(webApplicationLogger,
                                new Router(new Controller[]
                                {
                                    new OrdersController(logger, database),
                                    new ProductsController(logger, database)
                                }, webApplicationLogger))))));

            var simulationClientLogger = logger.ForContext("Application", "Roastery Automation Client").ForContext(typeof(Program));
            foreach (var product in await client.GetAsync<List<Product>>("api/products"))
            {
                simulationClientLogger.Information("Found product {@Product}", product);
            }

            try
            {
                await client.PostAsync<Order>("api/orders", new Order());
            }
            catch (Exception ex)
            {
                simulationClientLogger.Error(ex, "Could not create an order");
            }

            var created = await client.PostAsync<Order>("api/orders", new Order {CustomerName = "A. Customer", ShippingAddress = "123 A Street"});
            simulationClientLogger.Information("Created order {OrderId}", created.Id);
        }
    }
}
