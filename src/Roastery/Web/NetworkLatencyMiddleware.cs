using System.Threading.Tasks;
using Roastery.Util;

namespace Roastery.Web;

class NetworkLatencyMiddleware : HttpServer
{
    readonly HttpServer _next;

    public NetworkLatencyMiddleware(HttpServer next)
    {
        _next = next;
    }

    public override async Task<HttpResponse> InvokeAsync(HttpRequest request)
    {
        await Task.Delay(100 + (int)(Distribution.Uniform() * 300));
        var response = await _next.InvokeAsync(request);
        await Task.Delay(10 + (int) (Distribution.Uniform() * 100));
        return response;
    }
}