using System.Threading.Tasks;

namespace Roastery.Web
{
    abstract class HttpServer
    {
        public abstract Task<HttpResponse> InvokeAsync(HttpRequest request);
    }
}