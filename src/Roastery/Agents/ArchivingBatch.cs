using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Model;
using Roastery.Web;
using Serilog;
using Serilog.Context;

namespace Roastery.Agents
{
    class ArchivingBatch : Agent
    {
        readonly HttpClient _client;
        readonly ILogger _logger;

        public ArchivingBatch(HttpClient client, ILogger logger) 
            : base(30000)
        {
            _client = client;
            _logger = logger.ForContext<ArchivingBatch>();
        }

        protected override IEnumerable<Behavior> GetBehaviors()
        {
            yield return DeleteAbandonedCarts;
            yield return ArchiveShippedOrders;
        }

        async Task DeleteAbandonedCarts(CancellationToken cancellationToken)
        {
            using var _ = LogContext.PushProperty("BatchId", Guid.NewGuid());
            try
            {
                _logger.Information("Identifying abandoned orders");
                
                var orders = await _client.GetAsync<List<Order>>("api/orders");
                foreach (var order in orders)
                {
                    if (order.Status == OrderStatus.New)
                    {
                        var age = DateTime.UtcNow - order.CreatedAt;
                        _logger.Debug("Found unplaced order with age {Age}", age);

                        if (age > TimeSpan.FromSeconds(90))
                        {
                            _logger.Information("Order {OrderId} is abandoned; deleting", order.Id);
                            await _client.DeleteAsync($"api/orders/{order.Id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not check for abandoned orders");
            }
        }

        async Task ArchiveShippedOrders(CancellationToken cancellationToken)
        {
            using var _ = LogContext.PushProperty("BatchId", Guid.NewGuid());
            try
            {
                _logger.Information("Archiving completed orders");
                
                var orders = await _client.GetAsync<List<Order>>("api/orders");
                foreach (var order in orders)
                {
                    if (order.Status == OrderStatus.Shipped)
                    {
                        _logger.Information("Order {OrderId} has been shipped; archiving to long-term storage", order.Id);
                        await _client.DeleteAsync($"api/orders/{order.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Could not archive completed orders");
            }
        }
    }
}