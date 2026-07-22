using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Model;
using Roastery.Web;

namespace Roastery.Agents;

class WarehouseStaff : Agent
{
    const double ReorderThresholdKilograms = 300;

    readonly HttpClient _client;
    readonly LoadingDock _loadingDock;
    readonly ProductionSchedule _productionSchedule;

    public WarehouseStaff(HttpClient client, LoadingDock loadingDock, ProductionSchedule productionSchedule)
        : base(12000)
    {
        _client = client;
        _loadingDock = loadingDock;
        _productionSchedule = productionSchedule;
    }

    protected override IEnumerable<Behavior> GetBehaviors()
    {
        yield return ShipOrders;
        yield return RestockShelves;
    }

    async Task ShipOrders(CancellationToken cancellationToken)
    {
        var orders = await _client.GetAsync<List<Order>>("api/orders");
        foreach (var order in orders)
        {
            if (order.Status == OrderStatus.PendingShipment)
            {
                order.Status = OrderStatus.Shipped;
                await _client.PutAsync($"api/orders/{order.Id}", order);
            }
        }
    }

    async Task RestockShelves(CancellationToken cancellationToken)
    {
        var inventory = await _client.GetAsync<List<Inventory>>("api/inventory");

        foreach (var item in inventory)
        {
            if (item.QuantityKilograms < ReorderThresholdKilograms)
                _productionSchedule.Request(item.Blend);
        }

        foreach (var (blend, kilograms) in _loadingDock.Collect())
        {
            var item = inventory.FirstOrDefault(i => i.Blend == blend);
            if (item == null)
                continue;

            try
            {
                await _client.PostAsync<Inventory>($"api/inventory/{item.Id}/receipts",
                    new InventoryReceipt(Math.Round(kilograms, 2)));
            }
            catch
            {
                // The pallet couldn't be shelved; leave it on the dock for the
                // next collection
                _loadingDock.Deliver(blend, kilograms);
                throw;
            }
        }
    }
}