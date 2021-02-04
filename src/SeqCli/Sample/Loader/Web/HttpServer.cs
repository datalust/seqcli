using System.Threading.Tasks;

namespace SeqCli.Sample.Loader.Web
{
    abstract class HttpServer
    {
        public abstract Task<HttpResponse> InvokeAsync(HttpRequest request);
    }
}