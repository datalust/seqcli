using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Roastery.Data;
using Roastery.Model;
using Roastery.Web;
using Serilog;
using Serilog.Context;

// ReSharper disable UnusedType.Global

namespace Roastery.Api
{
    class OrdersController : Controller
    {
        readonly Database _database;

        public OrdersController(ILogger logger, Database database)
        : base(logger)
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
            var order = (Order) request.Body;

            if (order.CustomerName == null)
            {
                return BadRequest("To create an order, a customer name is required.");
            }
            
            await _database.InsertAsync(order);
            Log.Information("Created new order {OrderId} for customer {CustomerName}", order.Id, order.CustomerName);
            return Json(order, HttpStatusCode.Created);
        }

        [Route("PUT", "api/orders/{id}")]
        public async Task<HttpResponse> Update(HttpRequest request)
        {
            var order = (Order) request.Body;
            using var _ = LogContext.PushProperty("OrderId", order.Id);

            var existing = (await _database.SelectAsync<Order>(o => o.Id == order.Id, $"id = '{order.Id}'")).SingleOrDefault();
            if (existing == null)
                return NotFound();

            if (order.Status == existing.Status)
                return BadRequest($"The order is already in the {order.Status} state");
            
            await _database.UpdateAsync(order, $"status = '{order.Status}'");
            if (order.Status == OrderStatus.PendingShipment)
                Log.Information("Order placed and ready for shipment");
            else if (order.Status == OrderStatus.Shipped)
                Log.Information("Order shipped to {CustomerName} at {ShippingAddress}", order.CustomerName, order.ShippingAddress);
            else
                Log.Information("Order updated");
            
            return OK();
        }
        
        [Route("DELETE", "api/orders/{id}")]
        public async Task<HttpResponse> Delete(HttpRequest request)
        {
            var orderId = request.Path.Substring("/api/orders/".Length);
            using var _ = LogContext.PushProperty("OrderId", orderId);

            if (!(await _database.SelectAsync<Order>(o => o.Id == orderId, $"id = '{orderId}'")).Any())
                return NotFound();

            await _database.DeleteAsync<Order>(orderId);
            Log.Information("Order deleted");
            
            return OK();
        }
        
        [Route("POST", "api/orders/{id}/items")]
        public async Task<HttpResponse> AddItem(HttpRequest request)
        {
            var item = (OrderItem) request.Body;
            var order = (await _database.SelectAsync<Order>(o => o.Id == item.OrderId, $"id = '{item.OrderId}'")).SingleOrDefault();
            if (order == null)
                return NotFound();

            using var _ = LogContext.PushProperty("OrderId", order.Id);

            var product = (await _database.SelectAsync<Product>(p => p.Id == item.ProductId, $"id = '{item.ProductId}'")).SingleOrDefault();
            if (product == null)
                return NotFound();

            using var __ = LogContext.PushProperty("ProductId", product.Id);

            await _database.InsertAsync(item);
            Log.Information("Added 1 x product {@Product} to order", new { product.Name, product.SizeInGrams });

            return Json(item, HttpStatusCode.Created);
        }
    }
}
