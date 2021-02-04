using System.Net;
using System.Threading.Tasks;
using Roastery.Data;
using Roastery.Model;
using Roastery.Web;
using Serilog;

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
    }
}
