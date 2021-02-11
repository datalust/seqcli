using System.Threading.Tasks;
using Roastery.Data;
using Roastery.Model;
using Roastery.Web;
using Serilog;

// ReSharper disable UnusedMember.Global

namespace Roastery.Api
{
    class ProductsController : Controller
    {
        readonly Database _database;

        public ProductsController(ILogger logger, Database database)
        : base(logger)
        {
            _database = database;
        }

        [Route("GET", "api/products")]
        public async Task<HttpResponse> ListAsync(HttpRequest request)
        {
            return Json(await _database.SelectAsync<Product>());
        }
    }
}
