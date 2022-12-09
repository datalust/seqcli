using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roastery.Model;
using Roastery.Web;

namespace Roastery.Agents;

class WarehouseStaff : Agent
{
    readonly HttpClient _client;

    public WarehouseStaff(HttpClient client) 
        : base(20000)
    {
        _client = client;
    }

    protected override IEnumerable<Behavior> GetBehaviors()
    {
        yield return ShipOrders;
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
}