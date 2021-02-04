using System.Net;
using System.Threading.Tasks;

namespace SeqCli.Sample.Loader.Web
{
    abstract class Controller
    {
        protected static Task<HttpResponse> Json(object body)
        {
            return Task.FromResult(new HttpResponse(HttpStatusCode.OK, body));
        }
    }
}