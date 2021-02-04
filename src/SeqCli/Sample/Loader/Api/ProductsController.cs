using System.Collections.Generic;
using System.Threading.Tasks;
using SeqCli.Sample.Loader.Model;
using SeqCli.Sample.Loader.Web;

// ReSharper disable UnusedMember.Global

namespace SeqCli.Sample.Loader.Api
{
    class ProductsController : Controller
    {
        [Route("GET", "api/products")]
        public Task<HttpResponse> ListAsync(HttpRequest request)
        {
            return Json(new List<Product>
            {
                new Product("product-8908fd0sa", "BNZ1A", "Rocket Ship dark roast, whole beans 100g"),
                new Product("product-fsad890fj", "BNZ1B", "Rocket Ship dark roast, whole beans 250g"),
                new Product("product-fsdjkljrw", "BNZ1C", "Rocket Ship dark roast, whole beans 1kg"),
                new Product("product-2nkfkdsju", "GRD1A", "Rocket Ship dark roast, ground 100g"),
                new Product("product-f8sa9newq", "GRD1B", "Rocket Ship dark roast, ground 250g"),
                new Product("product-cvsad9033", "GRD1C", "Rocket Ship dark roast, ground 1kg"),
                new Product("product-i908jd0sf", "MRE1A", "1 AM medium roast, whole beans 100g"),
                new Product("product-isadj90fd", "MRE1B", "1 AM medium roast, whole beans 250g"),
                new Product("product-isdjjljr3", "MRE1C", "1 AM medium roast, whole beans 1kg"),
                new Product("product-inkfjdsj2", "MRG1A", "1 AM medium roast, ground 100g"),
                new Product("product-i8sajnew1", "MRG1B", "1 AM medium roast, ground 250g"),
                new Product("product-ivsaj903t", "MRG1C", "1 AM medium roast, ground 1kg")
            });
        }
    }
}
