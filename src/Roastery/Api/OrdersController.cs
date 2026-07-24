using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Roastery.Data;
using Roastery.Metrics;
using Roastery.Model;
using Roastery.Web;
using Serilog;
using Serilog.Context;

// ReSharper disable UnusedType.Global

namespace Roastery.Api;

class OrdersController : Controller
{
    readonly Database _database;

    public OrdersController(ILogger logger, RoasteryWebMetrics metrics, Database database)
        : base(logger, metrics)
    {
        _database = database;
    }

    [Route("GET", "api/orders")]
    public async Task<HttpResponse> List(HttpRequest request)
    {
        return Json(await _database.SelectAsync<Order>());
    }

    [Route("POST", "api/orders")]
    public async Task<HttpResponse> Create(HttpRequest request)
    {
        if (request.Body == null)
            return BadRequest("An order is required.");

        var order = (Order)request.Body;

        if (order.CustomerName == null)
        {
            return BadRequest("To create an order, a customer name is required.");
        }

        await _database.InsertAsync(order);

        Metrics.RecordOrderCreated();
        Log.Information("Created new order {OrderId} for customer {CustomerName}", order.Id, order.CustomerName);

        return Json(order, HttpStatusCode.Created);
    }

    [Route("PUT", "api/orders/{id}")]
    public async Task<HttpResponse> Update(HttpRequest request)
    {
        if (request.Body == null)
            return BadRequest("An order is required.");

        var order = (Order)request.Body;
        using var _ = LogContext.PushProperty("OrderId", order.Id);

        var existing =
            (await _database.SelectAsync<Order>(o => o.Id == order.Id, $"id = '{order.Id}'")).SingleOrDefault();
        if (existing == null)
            return NotFound();

        if (order.Status == existing.Status)
            return BadRequest($"The order is already in the {order.Status} state");

        await _database.UpdateAsync(order, $"status = '{order.Status}'");
        if (order.Status == OrderStatus.PendingShipment)
            Log.Information("Order placed and ready for shipment");
        else if (order.Status == OrderStatus.Shipped)
        {
            Metrics.RecordOrderShipped();
            Log.Information("Order shipped to {CustomerName} at {ShippingAddress}", order.CustomerName,
                order.ShippingAddress);
        }
        else
            Log.Information("Order updated");

        return OK();
    }

    [Route("DELETE", "api/orders/{id}")]
    public async Task<HttpResponse> Delete(HttpRequest request)
    {
        var orderId = request.Path.Substring("/api/orders/".Length);
        using var _ = LogContext.PushProperty("OrderId", orderId);

        var order = (await _database.SelectAsync<Order>(o => o.Id == orderId, $"id = '{orderId}'")).SingleOrDefault();
        if (order == null)
            return NotFound();

        // Items in unshipped orders have stock reserved for them; deleting the
        // order returns that stock to the warehouse
        if (order.Status != OrderStatus.Shipped)
            await ReturnItemsToStock(orderId);

        await _database.DeleteAsync<OrderItem>(o => o.OrderId == orderId, $"orderid = '{orderId}'");
        await _database.DeleteAsync<Order>(o => o.Id == orderId, $"id = '{orderId}'");
        Log.Information("Order deleted");

        return OK();
    }

    async Task ReturnItemsToStock(string orderId)
    {
        var items = await _database.SelectAsync<OrderItem>(i => i.OrderId == orderId, $"orderid = '{orderId}'");
        if (items.Count == 0)
            return;

        var products = (await _database.SelectAsync<Product>()).ToDictionary(p => p.Id!);

        foreach (var byBlend in items
                     .Where(i => products.ContainsKey(i.ProductId))
                     .GroupBy(i => products[i.ProductId].Blend))
        {
            var returnedKilograms = byBlend.Sum(i => products[i.ProductId].SizeInGrams) / 1000.0;

            var inventory =
                (await _database.SelectAsync<Inventory>(iv => iv.Blend == byBlend.Key, $"blend = '{byBlend.Key}'"))
                .SingleOrDefault();
            if (inventory == null)
                continue;

            inventory.QuantityKilograms = Math.Round(inventory.QuantityKilograms + returnedKilograms, 2);
            await _database.UpdateAsync(inventory,
                $"quantitykilograms = {inventory.QuantityKilograms.ToString(CultureInfo.InvariantCulture)}");

            Metrics.RecordStockLevel(new RoasteryWebMetrics.Sample.StockLevelKey(inventory.Blend),
                inventory.QuantityKilograms);
            Log.Information("Returned {ReturnedKilograms}kg of {Blend} to stock", returnedKilograms, byBlend.Key);
        }
    }

    [Route("POST", "api/orders/{id}/items")]
    public async Task<HttpResponse> AddItem(HttpRequest request)
    {
        if (request.Body == null)
            return BadRequest("An order item is required.");

        var item = (OrderItem)request.Body;
        var order = (await _database.SelectAsync<Order>(o => o.Id == item.OrderId, $"id = '{item.OrderId}'"))
            .SingleOrDefault();
        if (order == null)
            return NotFound();

        using var _ = LogContext.PushProperty("OrderId", order.Id);

        var product = (await _database.SelectAsync<Product>(p => p.Id == item.ProductId, $"id = '{item.ProductId}'"))
            .SingleOrDefault();
        if (product == null)
            return NotFound();

        using var __ = LogContext.PushProperty("ProductId", product.Id);

        var requiredKilograms = product.SizeInGrams / 1000.0;
        var inventory =
            (await _database.SelectAsync<Inventory>(i => i.Blend == product.Blend, $"blend = '{product.Blend}'"))
            .SingleOrDefault();
        if (inventory == null || inventory.QuantityKilograms < requiredKilograms)
        {
            Log.Warning("Product {ProductId} is out of stock", product.Id);
            return Conflict($"Insufficient stock of {product.Blend}.");
        }

        inventory.QuantityKilograms = Math.Round(inventory.QuantityKilograms - requiredKilograms, 2);
        await _database.UpdateAsync(inventory,
            $"quantitykilograms = {inventory.QuantityKilograms.ToString(CultureInfo.InvariantCulture)}");
        Metrics.RecordStockLevel(new RoasteryWebMetrics.Sample.StockLevelKey(inventory.Blend),
            inventory.QuantityKilograms);

        await _database.InsertAsync(item);
        Log.Information("Added 1 x product {@Product} to order", new { product.Name, product.SizeInGrams });

        return Json(item, HttpStatusCode.Created);
    }
}